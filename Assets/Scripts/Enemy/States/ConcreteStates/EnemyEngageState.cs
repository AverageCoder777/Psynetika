// Бой между атаками: враг следует тактике engage из конфига (преследование…),
// пока игрок в зоне агро, и уходит в атаку, как только она готова и игрок в зоне удара.
public class EnemyEngageState : EnemyStates
{
    private readonly EnemyTacticRuntime tactic;

    public EnemyEngageState(EnemyController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
        tactic = controller.Config.ResolveEngageTactic().CreateRuntime(controller);
    }

    public override void Enter()
    {
        tactic.Enter();
    }

    public override void LogicUpdate()
    {
        if (!Sensor.PlayerInFollowRange && !Sensor.PlayerInHitRange)
        {
            ChangeState(LostTargetStateId);
            return;
        }

        // На перезарядке продолжаем двигаться по тактике, а не стоим в зоне удара.
        if (Sensor.PlayerInHitRange && Attack.HasReadyAttack)
        {
            ChangeState(EnemyStateId.Attack);
            return;
        }

        UpdateLocomotionFlags();
    }

    public override void PhysicsUpdate()
    {
        Movement.Execute(tactic.Tick(UnityEngine.Time.fixedDeltaTime));
    }

    public override void Exit()
    {
        tactic.Exit();
        ClearLocomotionFlags();
    }
}
