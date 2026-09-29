using System;
using UnityEngine;

// Патруль туда-сюда по X вокруг точки спавна с паузами на краях маршрута.
// Разворачивается и на краю маршрута, и упершись в стену/обрыв/поводок.
[Serializable]
[AddTypeMenu("Патруль по линии")]
public class PatrolLineTactic : EnemyTactic
{
    [Tooltip("Половина маршрута: враг ходит от точки спавна на ±distance по X")]
    [Min(0.1f)] public float distance = 3f;

    [Tooltip("Пауза на краю маршрута, сек")]
    [Min(0f)] public float waitTime = 1f;

    [Tooltip("Доля от moveSpeed на патруле")]
    [Range(0.1f, 2f)] public float speedScale = 0.5f;

    public override EnemyTacticRuntime CreateRuntime(EnemyController owner) => new Runtime(this, owner);

    // Миграция старых конфигов, где патруль описывался блоком EnemyConfig.patrol.
    internal static PatrolLineTactic FromLegacy(EnemyPatrolSettings patrol) => new()
    {
        distance = patrol.distance,
        waitTime = patrol.waitTime,
        speedScale = patrol.speedScale
    };

    private class Runtime : EnemyTacticRuntime
    {
        private readonly PatrolLineTactic settings;
        private float targetX;
        private float waitLeft;

        public Runtime(PatrolLineTactic settings, EnemyController owner) : base(owner)
        {
            this.settings = settings;
        }

        public override void Enter()
        {
            waitLeft = 0f;
            targetX = EdgeX(Movement.FacingDirection >= 0f ? 1f : -1f);
        }

        public override MoveIntent Tick(float deltaTime)
        {
            if (waitLeft > 0f)
            {
                waitLeft -= deltaTime;
                return MoveIntent.Stop;
            }

            Vector2 target = new Vector2(targetX, Position.y);
            if (Movement.LastMoveBlocked || Movement.IsAt(target))
            {
                TurnAround();
                if (waitLeft > 0f) return MoveIntent.Stop;
                target = new Vector2(targetX, Position.y);
            }

            return MoveIntent.To(target, settings.speedScale);
        }

        private void TurnAround()
        {
            float newDirection = targetX > Position.x ? -1f : 1f;
            targetX = EdgeX(newDirection);
            Movement.Face(newDirection);
            waitLeft = settings.waitTime;
        }

        private float EdgeX(float direction) => Movement.SpawnPosition.x + direction * settings.distance;
    }
}
