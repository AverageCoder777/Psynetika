// Покой: игрок не замечен, враг следует тактике idle из конфига (стоять, патрулировать…)
// или маршруту EnemyPatrolPath, если он задан на этом враге в сцене.
public class EnemyRestState : EnemyStates
{
    private readonly EnemyTacticRuntime tactic;

    public EnemyRestState(EnemyController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
        tactic = controller.TryGetComponent(out EnemyPatrolPath path) && path.HasPoints
            ? path.CreateRuntime(controller)
            : controller.Config.ResolveIdleTactic().CreateRuntime(controller);
    }

    public override void Enter()
    {
        tactic.Enter();
    }

    public override void LogicUpdate()
    {
        // Один переход за кадр, приоритет атаке.
        if (TryReactToPlayer()) return;

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
