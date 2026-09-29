using System.Collections.Generic;
using UnityEngine;

/*
Маршрут патруля конкретного врага в сцене: «отсюда досюда».
EnemyConfig общий для всех врагов одного типа, поэтому точки живут не в нём, а на экземпляре.

Если компонент есть и в нём хотя бы одна точка, режим покоя ходит по этим точкам вместо
тактики idle из конфига. Одна точка = дойти до неё и стоять (пост охраны).

Точки хранятся смещениями от позиции врага: в редакторе маршрут двигается вместе с врагом,
в игре отсчитывается от точки спавна. Точки перетаскиваются мышкой в Scene View.
*/
[RequireComponent(typeof(EnemyController))]
public class EnemyPatrolPath : MonoBehaviour
{
    public enum PathMode
    {
        PingPong = 0,
        Loop = 1
    }

    [Tooltip("Смещения точек от позиции врага. Ходячим важен только X, летающим — обе оси")]
    public List<Vector2> points = new() { new Vector2(-3f, 0f), new Vector2(3f, 0f) };

    [Tooltip("PingPong — туда и обратно (A→B→C→B→A), Loop — по кругу (A→B→C→A)")]
    public PathMode mode = PathMode.PingPong;

    [Tooltip("Пауза в каждой точке, сек")]
    [Min(0f)] public float waitTime = 1f;

    [Tooltip("Доля от moveSpeed на маршруте")]
    [Range(0.1f, 2f)] public float speedScale = 0.5f;

    private EnemyMovement movement;

    public bool HasPoints => points != null && points.Count > 0;

    // В игре — от точки спавна (сам враг уже ушёл), в редакторе — от текущей позиции.
    public Vector2 Origin
    {
        get
        {
            if (Application.isPlaying)
            {
                if (movement == null) movement = GetComponent<EnemyMovement>();
                if (movement != null) return movement.SpawnPosition;
            }
            return transform.position;
        }
    }

    public Vector2 GetWorldPoint(int index) => Origin + points[index];

    public void SetWorldPoint(int index, Vector2 world) => points[index] = world - Origin;

    public EnemyTacticRuntime CreateRuntime(EnemyController owner) => new Runtime(this, owner);

    private class Runtime : EnemyTacticRuntime
    {
        private readonly EnemyPatrolPath path;
        private int index;
        private int step = 1;
        private float waitLeft;
        // Первый тик после входа: LastMoveBlocked ещё от прошлого состояния, верить ему нельзя.
        private bool fresh;

        public Runtime(EnemyPatrolPath path, EnemyController owner) : base(owner)
        {
            this.path = path;
        }

        public override void Enter()
        {
            index = NearestPointIndex();
            step = 1;
            waitLeft = 0f;
            fresh = true;
        }

        public override MoveIntent Tick(float deltaTime)
        {
            if (!path.HasPoints) return MoveIntent.Stop;

            if (waitLeft > 0f)
            {
                waitLeft -= deltaTime;
                return MoveIntent.Stop;
            }

            // Дошёл или упёрся (обрыв, стена, поводок) — следующая точка.
            Vector2 target = path.GetWorldPoint(index);
            bool blocked = !fresh && Movement.LastMoveBlocked;
            fresh = false;
            if (Movement.IsAt(target) || blocked)
            {
                if (path.points.Count == 1) return MoveIntent.Stop;

                Advance();
                target = path.GetWorldPoint(index);
                Movement.Face(target.x - Position.x);
                waitLeft = path.waitTime;
                if (waitLeft > 0f) return MoveIntent.Stop;
            }

            return MoveIntent.To(target, path.speedScale);
        }

        private void Advance()
        {
            int count = path.points.Count;
            if (path.mode == PathMode.Loop)
            {
                index = (index + 1) % count;
                return;
            }

            if (index + step < 0 || index + step >= count)
            {
                step = -step;
            }
            index += step;
        }

        private int NearestPointIndex()
        {
            int nearest = 0;
            float best = float.PositiveInfinity;
            for (int i = 0; i < path.points.Count; i++)
            {
                float distance = (path.GetWorldPoint(i) - Position).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = i;
                }
            }
            return nearest;
        }
    }

#if UNITY_EDITOR
    // Маршрут виден и у невыделенных врагов (бледно) — удобно смотреть все патрули уровня разом.
    private void OnDrawGizmos() => DrawPath(0.35f);

    private void OnDrawGizmosSelected() => DrawPath(1f);

    private void DrawPath(float alpha)
    {
        if (!HasPoints) return;

        Gizmos.color = new Color(0.3f, 0.85f, 1f, alpha);
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 point = GetWorldPoint(i);
            Gizmos.DrawWireSphere(point, 0.15f);
            if (i + 1 < points.Count)
            {
                Gizmos.DrawLine(point, GetWorldPoint(i + 1));
            }
        }
        if (mode == PathMode.Loop && points.Count > 2)
        {
            Gizmos.DrawLine(GetWorldPoint(points.Count - 1), GetWorldPoint(0));
        }
    }
#endif
}
