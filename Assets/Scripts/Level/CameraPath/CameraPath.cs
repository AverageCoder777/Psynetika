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
кривую. Каждый отрезок при этом бывает гибким (участвует в сглаживании) или строго прямым — длинный
ровный коридор не должен выгибаться из-за соседнего поворота. У каждой точки есть и запас вверх/вниз — там коридор расширяется, и камера может подняться
над линией (высокий зал, уступ, шахта), а потом плавно вернуться к ней.

Точки хранятся в локальных координатах объекта, поэтому весь путь двигается и масштабируется
вместе с ним. Сам коридор лежит дочерним объектом «Коридор (сгенерировано)» и пересобирается
целиком: правки внутри него потеряются при следующей сборке — правьте линию, а не коллайдеры.

Объект пути нарочно живёт вне корня сборки локации: LocationBuilderWindow при перестройке
удаляет всех детей корня, а линия должна пережить перестройку арта.

Зум на участке: у точек есть zoom (см. CameraPathPoint). В игре путь сам меняет OrthographicSize
камеры конфайнера: берёт её позицию, находит ближайшее место на линии и ставит зум этого места.
Позиция камеры, а не игрока — камера и так держится на линии, а коридор в каждом месте расширен
ровно под свой зум, поэтому кадр всегда помещается в коридор и конфайнер его не выталкивает.
После смены размера зовётся InvalidateLensCache: сам конфайнер смену обзора не замечает.
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

    [Tooltip("Плавность смены зума в игре, сек. 0 — зум меняется ровно по линии. Большая задержка " +
             "при приближении даёт кадру ненадолго быть шире коридора — конфайнер его подвинет")]
    [Min(0f)] public float zoomDamping;

    // Первая версия хранила точки голыми Vector2 в поле points. Держим старое поле, чтобы уже
    // нарисованные пути не пропали, и один раз переносим его в nodes (см. MigrateLegacyPoints).
    [SerializeField, HideInInspector, FormerlySerializedAs("points")]
    private List<Vector2> legacyPoints = new();

    // Зум в игре: камера, её исходный обзор и линия в мировых координатах с зумом каждой точки.
    private readonly List<Vector2> zoomLine = new();
    private readonly List<float> zoomValues = new();
    private CinemachineCamera zoomCamera;
    private float baseOrthographicSize;
    private float currentZoom = 1f;

    public Transform Corridor => transform.Find(CorridorName);

    // Есть ли на пути участки с нестандартным зумом.
    public bool HasZoom
    {
        get
        {
            if (nodes == null)
            {
                return false;
            }

            foreach (CameraPathPoint node in nodes)
            {
                if (!Mathf.Approximately(node.Zoom, 1f))
                {
                    return true;
                }
            }

            return false;
        }
    }

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

        // В игре обзор камеры меняет сам путь — ширину считаем по исходному, а не по текущему зуму.
        float size = zoomCamera != null
            ? baseOrthographicSize
            : owner is CinemachineCamera camera
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

    // Число отрезков: у замкнутого пути последний идёт от последней точки к первой.
    public int SegmentCount => nodes == null || nodes.Count < 2 ? 0 : closed ? nodes.Count : nodes.Count - 1;

    // Отрезок segment идёт от точки segment к следующей; режим хранится в его начальной точке.
    public bool IsSegmentStraight(int segment)
    {
        return NodeAt(segment).straight;
    }

    // Точка на отрезке в локальных координатах — та же, что попадёт в коридор (для ручек редактора).
    public Vector2 EvaluateSegment(int segment, float t)
    {
        CameraPathPoint from = NodeAt(segment);
        CameraPathPoint to = NodeAt(segment + 1);

        if (!IsCurved(segment))
        {
            return Vector2.Lerp(from.position, to.position, t);
        }

        GetControlPoints(segment, out Vector2 p0, out Vector2 p3);

        return CatmullRom(p0, from.position, to.position, p3, t);
    }

    /*
    Точки, по которым реально строится коридор. Прямой отрезок — это просто его два узла, гибкий —
    кривая Катмулла–Рома через узлы (при smoothing = 0 гибкие отрезки тоже прямые). Кривая проходит
    ровно через поставленные точки, поэтому дизайнер двигает понятные ему места, а не контрольные
    ручки. Запас up/down между узлами — линейно.
    */
    public List<CameraPathPoint> Sample()
    {
        List<CameraPathPoint> samples = new();

        if (nodes == null || nodes.Count == 0)
        {
            return samples;
        }

        int segments = SegmentCount;

        for (int i = 0; i < segments; i++)
        {
            CameraPathPoint from = NodeAt(i);

            if (!IsCurved(i))
            {
                samples.Add(from);
                continue;
            }

            GetControlPoints(i, out Vector2 p0, out Vector2 p3);
            CameraPathPoint to = NodeAt(i + 1);

            for (int step = 0; step <= smoothing; step++)
            {
                float t = step / (float)(smoothing + 1);
                CameraPathPoint sample = CameraPathPoint.Lerp(from, to, t);
                sample.position = CatmullRom(p0, from.position, to.position, p3, t);
                samples.Add(sample);
            }
        }

        if (!closed || segments == 0)
        {
            samples.Add(nodes[nodes.Count - 1]);
        }

        return samples;
    }

    private bool IsCurved(int segment)
    {
        return smoothing > 0 && nodes.Count >= 3 && !IsSegmentStraight(segment);
    }

    /*
    Внешние контрольные точки кривой для отрезка p1→p2.

    Обычно это соседние узлы. Но если сосед — прямой отрезок, кривая должна выйти из узла ровно
    по его направлению, иначе на стыке «гибкая → прямая» получится излом. Касательная Катмулла–Рома
    в p1 равна (p2 - p0) / 2, поэтому фиктивная p0 = p2 - 2L·d даёт касательную L·d вдоль прямой
    (L — длина отрезка, d — направление прямой). В p2 — симметрично.
    */
    private void GetControlPoints(int segment, out Vector2 p0, out Vector2 p3)
    {
        int count = nodes.Count;
        Vector2 p1 = NodeAt(segment).position;
        Vector2 p2 = NodeAt(segment + 1).position;
        float length = Vector2.Distance(p1, p2);

        bool hasPrevious = closed || segment > 0;
        bool hasNext = closed || segment + 1 < count - 1;

        p0 = NodeAt(segment - 1).position;
        p3 = NodeAt(segment + 2).position;

        if (hasPrevious && IsSegmentStraight(segment - 1))
        {
            Vector2 direction = (p1 - p0).normalized;

            if (direction != Vector2.zero)
            {
                p0 = p2 - direction * (2f * length);
            }
        }

        if (hasNext && IsSegmentStraight(segment + 1))
        {
            Vector2 direction = (p3 - p2).normalized;

            if (direction != Vector2.zero)
            {
                p3 = p1 + direction * (2f * length);
            }
        }
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
        FillMissingZoom();
    }

    private void Awake()
    {
        MigrateLegacyPoints();
        FillMissingZoom();
    }

    // Зум работает только в игре: в редакторе обзор камеры — это настройка, которую нельзя трогать.
    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            BeginZoom();
        }
    }

    // До LateUpdate: Cinemachine считает кадр в LateUpdate, и новый обзор попадает в тот же кадр.
    private void Update()
    {
        if (zoomCamera == null)
        {
            return;
        }

        float target = ZoomAt(zoomCamera.transform.position);

        currentZoom = zoomDamping > 0f
            ? Mathf.Lerp(currentZoom, target, 1f - Mathf.Exp(-Time.deltaTime / zoomDamping))
            : target;

        ApplyOrthographicSize(baseOrthographicSize * currentZoom);
    }

    // Выключили путь — камера возвращается к исходному обзору, а не застывает в чужом зуме.
    private void OnDisable()
    {
        if (zoomCamera == null)
        {
            return;
        }

        ApplyOrthographicSize(baseOrthographicSize);
        zoomCamera = null;
    }

    private void BeginZoom()
    {
        zoomCamera = null;

        if (!HasZoom || confiner == null || confiner.ComponentOwner is not CinemachineCamera camera)
        {
            return;
        }

        // Линия статична: семплы кривой считаются один раз, дальше только поиск ближайшей точки.
        zoomLine.Clear();
        zoomValues.Clear();

        foreach (CameraPathPoint sample in Sample())
        {
            zoomLine.Add(transform.TransformPoint(sample.position));
            zoomValues.Add(sample.Zoom);
        }

        if (zoomLine.Count == 0)
        {
            return;
        }

        zoomCamera = camera;
        baseOrthographicSize = camera.Lens.OrthographicSize;
        currentZoom = ZoomAt(camera.transform.position);
        ApplyOrthographicSize(baseOrthographicSize * currentZoom);
    }

    // Зум в ближайшем к точке месте линии: проекция на каждый отрезок, зум — линейно вдоль отрезка.
    private float ZoomAt(Vector2 point)
    {
        int count = zoomLine.Count;

        if (count == 1)
        {
            return zoomValues[0];
        }

        int segments = closed ? count : count - 1;
        float bestDistance = float.MaxValue;
        float bestZoom = 1f;

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % count;
            Vector2 from = zoomLine[i];
            Vector2 delta = zoomLine[next] - from;
            float lengthSqr = delta.sqrMagnitude;
            float t = lengthSqr <= Mathf.Epsilon ? 0f : Mathf.Clamp01(Vector2.Dot(point - from, delta) / lengthSqr);
            float distance = (from + delta * t - point).sqrMagnitude;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestZoom = Mathf.Lerp(zoomValues[i], zoomValues[next], t);
            }
        }

        return bestZoom;
    }

    private void ApplyOrthographicSize(float size)
    {
        if (Mathf.Approximately(zoomCamera.Lens.OrthographicSize, size))
        {
            return;
        }

        LensSettings lens = zoomCamera.Lens;
        lens.OrthographicSize = size;
        zoomCamera.Lens = lens;

        if (confiner != null)
        {
            confiner.InvalidateLensCache();
        }
    }

    // Точки, сохранённые до появления зума, читаются с нулём — в данных и в инспекторе это 1.
    private void FillMissingZoom()
    {
        if (nodes == null)
        {
            return;
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].zoom <= 0f)
            {
                CameraPathPoint node = nodes[i];
                node.zoom = 1f;
                nodes[i] = node;
            }
        }
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
