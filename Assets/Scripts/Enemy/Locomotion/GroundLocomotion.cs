using System;
using UnityEngine;

// Ходьба по земле: горизонталь задаёт враг, вертикаль — гравитация. Может останавливаться у обрыва и стены.
[Serializable]
[AddTypeMenu("Ходьба")]
public class GroundLocomotion : EnemyLocomotion
{
    [Tooltip("Не шагать за край платформы и не упираться в стену. Работает только при заполненном Ground Mask")]
    public bool stopAtLedges = false;

    [Tooltip("Слои земли/стен. Пусто = проверки выключены")]
    public LayerMask groundMask;

    [Tooltip("На какую глубину искать землю перед собой")]
    [Min(0.05f)] public float ledgeProbeDepth = 1f;

    [Tooltip("На каком расстоянии перед собой считать стену препятствием")]
    [Min(0f)] public float wallProbeDistance = 0.1f;

    [Tooltip("Точка считается достигнутой, если до неё по X меньше этого расстояния")]
    [Min(0.01f)] public float arriveDistance = 0.05f;

    public override EnemyLocomotionRuntime CreateRuntime(EnemyMovement movement) => new Runtime(this, movement);

    // Миграция старых конфигов, где проверки земли лежали в EnemyConfig.ground.
    internal static GroundLocomotion FromLegacy(EnemyGroundSettings ground) => ground == null
        ? new GroundLocomotion()
        : new GroundLocomotion
        {
            stopAtLedges = ground.stopAtLedges,
            groundMask = ground.groundMask,
            ledgeProbeDepth = ground.ledgeProbeDepth,
            wallProbeDistance = ground.wallProbeDistance
        };

    private class Runtime : EnemyLocomotionRuntime
    {
        private readonly GroundLocomotion settings;

        public Runtime(GroundLocomotion settings, EnemyMovement movement) : base(movement)
        {
            this.settings = settings;
        }

        public override void Attach()
        {
            // Ходячий не должен опрокидываться от толчков игрока и отбрасывания.
            Body.freezeRotation = true;
        }

        public override bool MoveTowards(Vector2 destination, float speed, float deltaTime)
        {
            float dx = destination.x - Body.position.x;
            if (Mathf.Abs(dx) <= settings.arriveDistance)
            {
                SetHorizontalVelocity(0f);
                return true;
            }

            // Поворачиваемся к цели, даже если шагнуть нельзя: у обрыва враг смотрит на игрока.
            float dir = dx > 0f ? 1f : -1f;
            movement.Face(dir);
            if (IsBlockedAhead(dir))
            {
                SetHorizontalVelocity(0f);
                return false;
            }

            // Не перелетаем точку за один шаг, иначе враг дрожит вокруг неё.
            float maxSpeed = deltaTime > 0f ? Mathf.Abs(dx) / deltaTime : speed;
            SetHorizontalVelocity(dir * Mathf.Min(speed, maxSpeed));
            return true;
        }

        public override void Stop() => SetHorizontalVelocity(0f);

        public override bool IsAt(Vector2 point) => Mathf.Abs(point.x - Body.position.x) <= settings.arriveDistance;

        private void SetHorizontalVelocity(float vx)
        {
            Body.linearVelocity = new Vector2(vx, Body.linearVelocity.y);
        }

        // Обрыв или стена по направлению dir. Выключено, пока не заполнен groundMask.
        private bool IsBlockedAhead(float dir)
        {
            if (!settings.stopAtLedges || settings.groundMask.value == 0) return false;

            Collider2D bodyCollider = movement.BodyCollider;
            Bounds bounds = bodyCollider != null
                ? bodyCollider.bounds
                : new Bounds(Body.position, new Vector3(0.5f, 1f, 0f));

            int mask = settings.groundMask.value;
            float edgeX = dir > 0f ? bounds.max.x : bounds.min.x;

            // Стена прямо по курсу.
            if (settings.wallProbeDistance > 0f)
            {
                Vector2 chest = new Vector2(edgeX, bounds.center.y);
                if (Physics2D.Raycast(chest, new Vector2(dir, 0f), settings.wallProbeDistance, mask))
                {
                    return true;
                }
            }

            // Земля под следующим шагом: луч чуть за краем коллайдера.
            Vector2 probe = new Vector2(edgeX + dir * 0.05f, bounds.min.y + 0.05f);
            return !Physics2D.Raycast(probe, Vector2.down, settings.ledgeProbeDepth, mask);
        }
    }
}
