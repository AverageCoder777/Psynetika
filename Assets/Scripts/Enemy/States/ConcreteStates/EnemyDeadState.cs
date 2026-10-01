using UnityEngine;

// Смерть как обычное состояние: предыдущее состояние корректно отрабатывает Exit()
// (сбрасывает флаги аниматора, останавливает движение), а не замирает посреди замаха.
// Эффекты смерти из конфига (взрыв тела…) запускаются здесь и продлевают жизнь тела, сколько им нужно.
public class EnemyDeadState : EnemyStates
{
    private static readonly int DieHash = Animator.StringToHash("Die");

    public EnemyDeadState(EnemyController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    public override void Enter()
    {
        PlayTrigger(DieHash);
        Sensor.DisableSensing();
        Movement.Stop();

        float delay = Config != null ? Config.deathDespawnDelay : 0f;
        if (Config != null && Config.deathEffects != null)
        {
            foreach (EnemyDeathEffect effect in Config.deathEffects)
            {
                if (effect != null)
                {
                    delay = Mathf.Max(delay, effect.Begin(controller));
                }
            }
        }
        Object.Destroy(controller.gameObject, delay);
    }
}
