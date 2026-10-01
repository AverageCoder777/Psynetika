using System;
using UnityEngine;
using Random = UnityEngine.Random;

// Блуждание: короткие перебежки к случайным точкам вокруг точки спавна с паузами между ними.
[Serializable]
[AddTypeMenu("Блуждание")]
public class WanderTactic : EnemyTactic
{
    [Tooltip("Точки выбираются в пределах ±radius от точки спавна по X")]
    [Min(0.1f)] public float radius = 3f;

    [Tooltip("Доля от moveSpeed при перебежках")]
    [Range(0.1f, 3f)] public float speedScale = 0.6f;

    [Tooltip("Пауза в точке: от min до max, сек")]
    [Min(0f)] public float pauseMin = 0.2f;
    [Min(0f)] public float pauseMax = 1.2f;

    [Tooltip("Разброс точек по Y — для летающих тел; наземному телу Y не важен")]
    [Min(0f)] public float verticalSpread = 0f;

    public override EnemyTacticRuntime CreateRuntime(EnemyController owner) => new Runtime(this, owner);

    private class Runtime : EnemyTacticRuntime
    {
        private readonly WanderTactic settings;
        private Vector2 point;
        private bool hasPoint;
        private float waitLeft;

        public Runtime(WanderTactic settings, EnemyController owner) : base(owner)
        {
            this.settings = settings;
        }

        public override void Enter()
        {
            hasPoint = false;
            waitLeft = 0f;
        }

        public override MoveIntent Tick(float deltaTime)
        {
            if (waitLeft > 0f)
            {
                waitLeft -= deltaTime;
                return MoveIntent.Stop;
            }

            if (!hasPoint)
            {
                PickPoint();
            }
            else if (Movement.LastMoveBlocked || Movement.IsAt(point))
            {
                hasPoint = false;
                waitLeft = Random.Range(
                    Mathf.Min(settings.pauseMin, settings.pauseMax),
                    Mathf.Max(settings.pauseMin, settings.pauseMax));
                return MoveIntent.Stop;
            }

            return MoveIntent.To(point, settings.speedScale);
        }

        private void PickPoint()
        {
            point = Movement.SpawnPosition + new Vector2(
                Random.Range(-settings.radius, settings.radius),
                Random.Range(-settings.verticalSpread, settings.verticalSpread));
            hasPoint = true;
        }
    }
}
