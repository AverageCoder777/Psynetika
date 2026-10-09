using System.Collections.Generic;
using UnityEngine;

/*
Верёвка уровня. Точка крепления — позиция объекта, верёвка висит вниз на length.
Физических коллайдеров у неё нет: форма симулируется Верле только для отрисовки и поиска точки хвата.

Раскачку героя считает RopeState (маятник). Пока героя держат, верёвка натянута прямой
от крепления до руки, а ниже руки свободно болтается хвост. После отпускания верёвка
продолжает качаться по инерции сама.

Крепление можно повесить дочерним объектом на движущуюся платформу — anchor берётся из transform каждый шаг.
*/
[RequireComponent(typeof(LineRenderer))]
public class Rope : MonoBehaviour
{
    private static readonly List<Rope> active = new();

    [Header("Форма")]
    [SerializeField, Min(0.5f)] private float length = 5f;
    [SerializeField, Range(4, 64)] private int segmentCount = 20;

    [Header("Хват")]
    [Tooltip("Насколько близко к верёвке должна оказаться рука героя, чтобы зацепиться.")]
    [SerializeField, Min(0.05f)] private float grabRadius = 0.5f;
    [Tooltip("Ближе к креплению висеть нельзя — короткий маятник раскачивается слишком резко.")]
    [SerializeField, Min(0.1f)] private float minHoldDistance = 0.75f;

    [Header("Симуляция (только визуал)")]
    [SerializeField, Min(0f)] private float gravityScale = 1f;
    [SerializeField, Range(0f, 0.2f)] private float damping = 0.02f;
    [SerializeField, Range(1, 50)] private int constraintIterations = 20;

    private LineRenderer line;
    private Vector2[] points;
    private Vector2[] previous;
    private float segmentLength;

    private Transform holder;
    private Vector2 holderOffset;
    private float holdDistance;

    public Vector2 Anchor => transform.position;
    public float Length => length;
    public float MinHoldDistance => Mathf.Min(minHoldDistance, length);
    public bool IsHeld => holder != null;

    /// <summary>
    /// Ближайшая к точке руки свободная верёвка в пределах её grabRadius.
    /// ignored — верёвка, которую только что отпустили (защита от мгновенного перехвата).
    /// </summary>
    public static bool TryFindGrabbable(Vector2 hand, Rope ignored, out Rope rope)
    {
        rope = null;
        float bestSqrDistance = float.MaxValue;
        for (int i = 0; i < active.Count; i++)
        {
            Rope candidate = active[i];
            if (candidate == ignored || candidate.IsHeld)
            {
                continue;
            }

            float sqrDistance = candidate.SqrDistanceTo(hand);
            if (sqrDistance <= candidate.grabRadius * candidate.grabRadius && sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                rope = candidate;
            }
        }
        return rope != null;
    }

    public void Attach(Transform who, Vector2 handOffset, float distance)
    {
        holder = who;
        holderOffset = handOffset;
        SetHoldDistance(distance);
    }

    public void SetHoldDistance(float distance)
    {
        holdDistance = Mathf.Clamp(distance, MinHoldDistance, length);
    }

    public void Detach()
    {
        holder = null;
    }

    #region Unity
    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = segmentCount + 1;

        segmentLength = length / segmentCount;
        points = new Vector2[segmentCount + 1];
        previous = new Vector2[segmentCount + 1];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = previous[i] = Anchor + Vector2.down * (segmentLength * i);
        }
    }

    private void OnEnable() => active.Add(this);

    private void OnDisable()
    {
        active.Remove(this);
        holder = null;
    }

    private void FixedUpdate()
    {
        int pinnedUpTo = Mathf.Max(0, HeldIndex());
        PinHeldPart(pinnedUpTo);

        float dt = Time.fixedDeltaTime;
        Vector2 gravityStep = Physics2D.gravity * (gravityScale * dt * dt);
        for (int i = pinnedUpTo + 1; i < points.Length; i++)
        {
            Vector2 velocity = (points[i] - previous[i]) * (1f - damping);
            previous[i] = points[i];
            points[i] += velocity + gravityStep;
        }

        for (int iteration = 0; iteration < constraintIterations; iteration++)
        {
            SolveConstraints(pinnedUpTo);
        }
    }

    private void LateUpdate()
    {
        // Натянутую часть рисуем от актуальной позиции руки, а не из FixedUpdate — без отставания на кадр.
        int held = HeldIndex();
        Vector2 hand = held > 0 ? HandPosition() : Anchor;
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 point = i <= held ? Vector2.Lerp(Anchor, hand, (float)i / held) : points[i];
            line.SetPosition(i, point);
        }
    }
    #endregion

    #region Simulation
    private Vector2 HandPosition() => (Vector2)holder.position + holderOffset;

    // Индекс точки, за которую держатся (-1 — верёвка свободна).
    private int HeldIndex()
    {
        if (holder == null)
        {
            return -1;
        }
        return Mathf.Clamp(Mathf.RoundToInt(holdDistance / segmentLength), 1, segmentCount);
    }

    // Участок 0..pinnedUpTo кладётся на прямую «крепление → рука». previous хранит старую позицию,
    // чтобы после отпускания эти точки полетели со скоростью руки.
    private void PinHeldPart(int pinnedUpTo)
    {
        Vector2 end = pinnedUpTo > 0 ? HandPosition() : Anchor;
        for (int i = 0; i <= pinnedUpTo; i++)
        {
            previous[i] = points[i];
            points[i] = pinnedUpTo > 0 ? Vector2.Lerp(Anchor, end, (float)i / pinnedUpTo) : Anchor;
        }
    }

    private void SolveConstraints(int pinnedUpTo)
    {
        for (int i = Mathf.Max(0, pinnedUpTo); i < points.Length - 1; i++)
        {
            Vector2 delta = points[i + 1] - points[i];
            float distance = delta.magnitude;
            if (distance < 1e-6f)
            {
                continue;
            }

            Vector2 correction = delta * ((distance - segmentLength) / distance);
            if (i <= pinnedUpTo)
            {
                points[i + 1] -= correction;
            }
            else
            {
                points[i] += correction * 0.5f;
                points[i + 1] -= correction * 0.5f;
            }
        }
    }

    private float SqrDistanceTo(Vector2 point)
    {
        // Быстрый отсев: дальше длины верёвки от крепления дотянуться нельзя.
        float reach = length + grabRadius;
        if ((point - Anchor).sqrMagnitude > reach * reach)
        {
            return float.MaxValue;
        }

        float best = float.MaxValue;
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 a = points[i];
            Vector2 ab = points[i + 1] - a;
            float sqrLength = ab.sqrMagnitude;
            float t = sqrLength > 1e-8f ? Mathf.Clamp01(Vector2.Dot(point - a, ab) / sqrLength) : 0f;
            best = Mathf.Min(best, (point - (a + ab * t)).sqrMagnitude);
        }
        return best;
    }
    #endregion

    #region Editor
#if UNITY_EDITOR
    private const string SpriteUnlitMaterialPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

    private void Reset()
    {
        LineRenderer lr = GetComponent<LineRenderer>();
        lr.widthMultiplier = 0.08f;
        lr.numCapVertices = 2;
        lr.startColor = lr.endColor = new Color(0.55f, 0.4f, 0.25f);
        if (lr.sharedMaterial == null)
        {
            lr.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
        }
        OnValidate();
    }

    // В редакторе показываем верёвку прямой, чтобы её было видно при расстановке уровня.
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        LineRenderer lr = GetComponent<LineRenderer>();
        if (lr == null)
        {
            return;
        }
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.SetPosition(0, Vector3.zero);
        lr.SetPosition(1, Vector3.down * length);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 anchor = transform.position;
        Vector3 end = anchor + Vector3.down * length;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(anchor, end);
        Gizmos.DrawWireSphere(anchor, 0.1f);

        // Зона хвата и дуга максимальной раскачки (примерно, без учёта настроек героя).
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.6f);
        Gizmos.DrawWireSphere(end, grabRadius);
        UnityEditor.Handles.color = Gizmos.color;
        UnityEditor.Handles.DrawWireArc(anchor, Vector3.forward, Quaternion.Euler(0, 0, -80f) * Vector3.down, 160f, length);
    }
#endif
    #endregion
}
