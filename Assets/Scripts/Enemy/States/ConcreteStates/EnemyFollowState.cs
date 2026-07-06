using UnityEngine;

public class EnemyFollowState : EnemyStates
{
    private static readonly int WalkingHash = Animator.StringToHash("Walking");

    public EnemyFollowState(EnemyController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    public override void Enter()
    {
        Animator.SetBool(WalkingHash, true);
    }

    public override void LogicUpdate()
    {
        if (!Sensor.PlayerInFollowRange)
        {
            stateMachine.ChangeState(controller.IdleState);
            return;
        }
        if (Sensor.PlayerInHitRange)
        {
            stateMachine.ChangeState(controller.AttackState);
        }
    }

    public override void PhysicsUpdate()
    {
        Transform target = Sensor.PlayerTransform;
        if (target != null)
        {
            Movement.MoveTowardsX(target.position.x);
        }
    }

    public override void Exit()
    {
        Animator.SetBool(WalkingHash, false);
    }
}
