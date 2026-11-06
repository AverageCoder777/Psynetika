using System;
using UnityEngine;

/*
Лазание без гравитации: враг идёт к точке напрямую в 2D — вверх по стене, по потолку, по земле.
Тело кинематическое: стены и платформы его не держат и не толкают, маршрут целиком задаёт тот,
кто выдаёт намерения (босс «Мама паук» ползёт по точкам арены). Пули игрока в него попадают как обычно.
*/
[Serializable]
[AddTypeMenu("Лазание по стенам (без гравитации)")]
public class CrawlLocomotion : EnemyLocomotion
{
    [Tooltip("Точка считается достигнутой, если до неё меньше этого расстояния")]
    [Min(0.01f)] public float arriveDistance = 0.05f;

    public override EnemyLocomotionRuntime CreateRuntime(EnemyMovement movement) => new Runtime(this, movement);

    private class Runtime : EnemyLocomotionRuntime
    {
        private readonly CrawlLocomotion settings;

        public Runtime(CrawlLocomotion settings, EnemyMovement movement) : base(movement)
        {
            this.settings = settings;
        }

        public override void Attach()
        {
            Body.bodyType = RigidbodyType2D.Kinematic;
            Body.gravityScale = 0f;
            Body.linearVelocity = Vector2.zero;
        }

        public override bool MoveTowards(Vector2 destination, float speed, float deltaTime)
        {
            Vector2 delta = destination - Body.position;
            float distance = delta.magnitude;
            if (distance <= settings.arriveDistance)
            {
                Body.linearVelocity = Vector2.zero;
                return true;
            }

            // Разворачиваемся только при ходьбе по горизонтали: на стене «вперёд» — это вверх или вниз.
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                movement.Face(delta.x);
            }

            // Не перелетаем точку за один шаг, иначе враг дрожит вокруг неё.
            float maxSpeed = deltaTime > 0f ? distance / deltaTime : speed;
            Body.linearVelocity = delta / distance * Mathf.Min(speed, maxSpeed);
            return true;
        }

        public override void Stop() => Body.linearVelocity = Vector2.zero;

        public override bool IsAt(Vector2 point) => (point - Body.position).sqrMagnitude <= settings.arriveDistance * settings.arriveDistance;
    }
}
