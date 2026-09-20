using UnityEngine;

// Преследование: идти к игроку по X, пока он в зоне агро.
public class EnemyFollowState : EnemyStates
{
    private static readonly int WalkingHash = Animator.StringToHash("Walking");

    public EnemyFollowState(EnemyController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    public override void Enter()
    {
        SetFlag(WalkingHash, true);
    }

    public override void LogicUpdate()
    {
        if (!Sensor.PlayerInFollowRange && !Sensor.PlayerInHitRange)
        {
            ChangeState(RestStateId);
            return;
        }

        // На перезарядке продолжаем преследовать, а не стоим в зоне удара.
        if (Sensor.PlayerInHitRange && Attack.HasReadyAttack)
        {
            ChangeState(EnemyStateId.Attack);
        }
    }

    public override void PhysicsUpdate()
    {
        Transform target = Sensor.PlayerTransform;
        if (target == null) return;

        if (!Movement.MoveTowardsX(target.position.x))
        {
            // Обрыв или стена между врагом и игроком: дальше не идём, но цель не теряем.
            Movement.StopHorizontal();
        }
    }

    public override void Exit()
    {
        SetFlag(WalkingHash, false);
    }
}
