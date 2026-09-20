using UnityEngine;

// Смерть как обычное состояние: предыдущее состояние корректно отрабатывает Exit()
// (сбрасывает флаги аниматора, останавливает движение), а не замирает посреди замаха.
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
        Movement.StopHorizontal();

        float delay = Config != null ? Config.deathDespawnDelay : 0f;
        Object.Destroy(controller.gameObject, delay);
    }
}
