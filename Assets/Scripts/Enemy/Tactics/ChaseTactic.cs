using System;
using UnityEngine;

// Преследование: идти к игроку. Наземное тело упрётся в обрыв/стену и будет ждать там, не теряя цель.
[Serializable]
[AddTypeMenu("Преследование")]
public class ChaseTactic : EnemyTactic
{
    [Tooltip("Доля от moveSpeed при преследовании")]
    [Range(0.1f, 3f)] public float speedScale = 1f;

    public override EnemyTacticRuntime CreateRuntime(EnemyController owner) => new Runtime(this, owner);

    private class Runtime : EnemyTacticRuntime
    {
        private readonly ChaseTactic settings;

        public Runtime(ChaseTactic settings, EnemyController owner) : base(owner)
        {
            this.settings = settings;
        }

        public override MoveIntent Tick(float deltaTime)
        {
            Transform target = Sensor.PlayerTransform;
            return target != null ? MoveIntent.To(target.position, settings.speedScale) : MoveIntent.Stop;
        }
    }
}
