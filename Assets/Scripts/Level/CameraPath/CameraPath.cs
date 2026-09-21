using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

/*
Путь камеры: линия, нарисованная прямо в сцене, по которой строится коридор для
CinemachineConfiner2D.

Зачем: прямоугольные границы уровня годятся, пока уровень — один экран. Как только он становится
длинным и с перепадами, прямоугольник пускает камеру в пустоту над крышами и под полом. Коридор
шириной в кадр держит камеру на линии, которую поставил дизайнер, и при этом не требует рисовать
аккуратную замкнутую фигуру руками — достаточно провести линию.

Линия не обязана быть прямой: точки ставятся где угодно, «Сглаживание» превращает ломаную в плавную
кривую, а у каждой точки есть запас вверх/вниз — там коридор расширяется, и камера может подняться
над линией (высокий зал, уступ, шахта), а потом плавно вернуться к ней.

Точки хранятся в локальных координатах объекта, поэтому весь путь двигается и масштабируется
вместе с ним. Сам коридор лежит дочерним объектом «Коридор (сгенерировано)» и пересобирается
целиком: правки внутри него потеряются при следующей сборке — правьте линию, а не коллайдеры.

Объект пути нарочно живёт вне корня сборки локации: LocationBuilderWindow при перестройке
удаляет всех детей корня, а линия должна пережить перестройку арта.
*/
[DisallowMultipleComponent]
public class CameraPath : MonoBehaviour
{
    public const string CorridorName = "Коридор (сгенерировано)";

    [Tooltip("Точки пути в локальных координатах и запас вверх/вниз у каждой. Правятся ручками " +
             "в Scene View: Shift+клик — добавить, Ctrl+клик — удалить, квадратики над/под точкой — запас")]
    public List<CameraPathPoint> nodes = new();

    [Tooltip("Замкнуть путь: последняя точка соединяется с первой")]
    public bool closed;

    [Tooltip("Сглаживание: сколько промежуточных точек кривой ставить на каждый отрезок. " +
             "0 — прямые отрезки, 4–8 — плавная кривая через те же точки")]
    [Range(0, 16)] public int smoothing = 6;

    [Tooltip("Брать ширину коридора из обзора камеры: тогда камера едет ровно по линии")]
    public bool widthFromCamera = true;

    [Tooltip("Запас сверх обзора камеры, в юнитах: насколько камере позволено отходить от линии")]
    [Min(0f)] public float slack;

    [Tooltip("Ширина коридора в юнитах, когда обзор камеры не используется")]
    [Min(0.01f)] public float width = 10f;

    [Tooltip("Конфайнер, которому отдаётся построенный коридор")]
    public CinemachineConfiner2D confiner;

    [Tooltip("Слой физики объекта коридора. Коридор — триггер и в физику уровня не вмешивается")]
    [PhysicsLayerName] public string physicsLayer = "Ignore Raycast";

    // Первая версия хранила точки голыми Vector2 в поле points. Держим старое поле, чтобы уже
    // нарисованные пути не пропали, и один раз переносим его в nodes (см. MigrateLegacyPoints).
    [SerializeField, HideInInspector, FormerlySerializedAs("points")]
    private List<Vector2> legacyPoints = new();

    public Transform Corridor => transform.Find(CorridorName);

    public bool HasCorridor => Corridor != null;

    // Ширина горизонтального участка — её окно и инспектор показывают как «ширину коридора».
    public float ResolveWidth()
    {
        return ResolveWidth(Vector2.right);
    }

    /*
    Ширина коридора поперёк отрезка с направлением direction.

    Кадр — прямоугольник, поэтому поперёк горизонтального участка нужна высота кадра, поперёк
    вертикального — его ширина, а на диагонали — проекция кадра на нормаль отрезка:
    2 * (|n.x| * halfWidth + |n.y| * halfHeight). Коридор такой ширины держит камеру ровно на линии.
    */
    public float ResolveWidth(Vector2 direction)
    {
        if (!widthFromCamera || !TryGetCameraSize(out float halfWidth, out float halfHeight))
        {
            return Mathf.Max(0.01f, width);
        }

        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = Vector2.right;
        }

        Vector2 normal = new Vector2(-direction.y, direction.x).normalized;
        float extent = 2f * (Mathf.Abs(normal.x) * halfWidth + Mathf.Abs(normal.y) * halfHeight);

        return Mathf.Max(0.01f, extent + slack);
    }

    /*
    Половины кадра в юнитах. Размер берём у виртуальной камеры, к которой прикручен конфайнер:
    именно её конфайнер и будет вписывать в коридор. Игра ортографическая: половина высоты =
    OrthographicSize. Пропорции — у выходной камеры (Camera.main), без неё считаем 16:9.
    */
    public bool TryGetCameraSize(out float halfWidth, out float halfHeight)
    {
        halfWidth = 0f;
        halfHeight = 0f;

        if (confiner == null)
        {
            return false;
        }

        CinemachineVirtualCameraBase owner = confiner.ComponentOwner;

        if (owner == null)
        {
            return false;
        }

        float size = owner is CinemachineCamera camera
            ? camera.Lens.OrthographicSize
            : owner.State.Lens.OrthographicSize;

        if (size <= 0f)
        {
            return false;
        }

        Camera output = Camera.main;
        float aspect = output != null && output.aspect > 0f ? output.aspect : 16f / 9f;

        halfHeight = size;
        halfWidth = size * aspect;

        return true;
    }

    public Vector3 GetWorldPoint(int index)
    {
        return transform.TransformPoint(nodes[index].position);
    }

    public void SetWorldPoint(int index, Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        CameraPathPoint node = nodes[index];
        node.position = new Vector2(local.x, local.y);
        nodes[index] = node;
    }

    /*
    Точки, по которым реально строится коридор: сами узлы при smoothing = 0, иначе кривая
    Катмулла–Рома через узлы. Кривая проходит ровно через поставленные точки, поэтому дизайнер
    двигает понятные ему места, а не контрольные ручки. Запас up/down между узлами — линейно.
    */
    public List<CameraPathPoint> Sample()
    {
        List<CameraPathPoint> samples = new();

        if (nodes == null || nodes.Count == 0)
        {
            return samples;
        }

        if (smoothing <= 0 || nodes.Count < 3)
        {
            samples.AddRange(nodes);
            return samples;
        }

        int count = nodes.Count;
        int segments = closed ? count : count - 1;

        for (int i = 0; i < segments; i++)
        {
            Vector2 p0 = NodeAt(i - 1).position;
            Vector2 p1 = NodeAt(i).position;
            Vector2 p2 = NodeAt(i + 1).position;
            Vector2 p3 = NodeAt(i + 2).position;

            for (int step = 0; step <= smoothing; step++)
            {
                float t = step / (float)(smoothing + 1);
                CameraPathPoint sample = CameraPathPoint.Lerp(NodeAt(i), NodeAt(i + 1), t);
                sample.position = CatmullRom(p0, p1, p2, p3, t);
                samples.Add(sample);
            }
        }

        if (!closed)
        {
            samples.Add(nodes[count - 1]);
        }

        return samples;
    }

    // Узел по индексу: у замкнутого пути индексы заворачиваются, у открытого — упираются в концы.
    private CameraPathPoint NodeAt(int index)
    {
        int count = nodes.Count;

        return closed
            ? nodes[((index % count) + count) % count]
            : nodes[Mathf.Clamp(index, 0, count - 1)];
    }

    private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (2f * p1
            + (p2 - p0) * t
            + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
            + (3f * p1 - p0 - 3f * p2 + p3) * t3);
    }

    /*
    Пересобрать коридор и отдать его конфайнеру.

    created/destroying — точки, через которые редактор подключает Undo: у рантайм-кода
    доступа к Undo нет, а создавать объекты сцены мимо него нельзя (см. LocationBuildContext).
    */
    public Collider2D Build(Action<GameObject> created = null, Action<GameObject> destroying = null)
    {
        ClearCorridor(destroying);

        if (nodes == null || nodes.Count < 2)
        {
            Debug.LogWarning($"{name}: в пути камеры меньше двух точек, коридор не построен", this);
            return null;
        }

        GameObject target = new(CorridorName);
        target.transform.SetParent(transform, false);
        target.layer = ResolveLayer();

        created?.Invoke(target);

        Collider2D collider = CameraCorridorFactory.Build(target, Sample(), ResolveWidth, closed);

        if (collider == null)
        {
            Debug.LogWarning($"{name}: все отрезки пути нулевой длины, коридор не построен", this);
            DestroyObject(target, destroying);

            return null;
        }

        if (confiner == null)
        {
            Debug.LogWarning($"{name}: коридор построен, но CinemachineConfiner2D не задан — подключите вручную", this);
            return collider;
        }

        confiner.BoundingShape2D = collider;
        confiner.InvalidateBoundingShapeCache();

        return collider;
    }

    public void ClearCorridor(Action<GameObject> destroying = null)
    {
        Transform corridor = Corridor;

        if (corridor != null)
        {
            DestroyObject(corridor.gameObject, destroying);
        }
    }

    [ContextMenu("Построить коридор")]
    private void BuildFromMenu()
    {
        Build();
    }

    private void Reset()
    {
        nodes = new List<CameraPathPoint> { new(new Vector2(-10f, 0f)), new(new Vector2(10f, 0f)) };
        legacyPoints = new List<Vector2>();
        confiner = FindFirstObjectByType<CinemachineConfiner2D>(FindObjectsInactive.Include);
    }

    private void OnValidate()
    {
        MigrateLegacyPoints();
    }

    private void Awake()
    {
        MigrateLegacyPoints();
    }

    private void MigrateLegacyPoints()
    {
        if (legacyPoints == null || legacyPoints.Count == 0)
        {
            return;
        }

        if (nodes == null || nodes.Count == 0)
        {
            nodes = new List<CameraPathPoint>(legacyPoints.Count);

            foreach (Vector2 point in legacyPoints)
            {
                nodes.Add(new CameraPathPoint(point));
            }
        }

        legacyPoints.Clear();
    }

    private int ResolveLayer()
    {
        int index = string.IsNullOrWhiteSpace(physicsLayer) ? 0 : LayerMask.NameToLayer(physicsLayer);

        if (index >= 0)
        {
            return index;
        }

        Debug.LogWarning($"{name}: в проекте нет слоя физики «{physicsLayer}», коридор остался на Default", this);

        return 0;
    }

    private static void DestroyObject(GameObject target, Action<GameObject> destroying)
    {
        if (destroying != null)
        {
            destroying(target);
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private void OnDrawGizmos()
    {
        if (nodes == null || nodes.Count < 2)
        {
            return;
        }

        List<CameraPathPoint> samples = Sample();
        float[] widths = CameraCorridorFactory.SegmentWidths(samples, ResolveWidth, closed);

        Gizmos.matrix = transform.localToWorldMatrix;

        for (int i = 0; i < widths.Length; i++)
        {
            CameraPathPoint from = samples[i];
            CameraPathPoint to = samples[(i + 1) % samples.Count];

            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.9f);
            Gizmos.DrawLine(from.position, to.position);

            if (widths[i] <= 0f)
            {
                continue;
            }

            // Только верхний и нижний края: полный контур каждого куска превращает гизмо в кашу.
            Vector2[] polygon = CameraCorridorFactory.SegmentPolygon(from, to, widths[i]);

            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.35f);
            Gizmos.DrawLine(polygon[0], polygon[1]);
            Gizmos.DrawLine(polygon[3], polygon[2]);
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}
