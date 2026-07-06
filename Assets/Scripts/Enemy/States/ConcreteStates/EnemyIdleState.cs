using UnityEngine;

public class EnemyIdleState : EnemyStates
{
    private static readonly int IdleHash = Animator.StringToHash("Idle");

    public EnemyIdleState(EnemyController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    public override void Enter()
    {
        Animator.SetBool(IdleHash, true);
    }

    public override void LogicUpdate()
    {
        // Один переход за кадр, приоритет атаке.
        if (Sensor.PlayerInHitRange)
        {
            stateMachine.ChangeState(controller.AttackState);
        }
        else if (Sensor.PlayerInFollowRange)
        {
            stateMachine.ChangeState(controller.FollowState);
        }
    }

    public override void Exit()
    {
        Animator.SetBool(IdleHash, false);
    }
}
