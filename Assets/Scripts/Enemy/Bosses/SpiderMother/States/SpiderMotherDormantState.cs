// Мама паук до боя (роль Rest): сидит на стартовой точке стены, пока арена не начнёт бой.
public class SpiderMotherDormantState : EnemyStates
{
    private readonly SpiderMotherController boss;

    public SpiderMotherDormantState(SpiderMotherController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
        boss = controller;
    }

    public override void Enter()
    {
        ClearLocomotionFlags();
        Movement.Stop();

        SpiderSpot start = boss.Arena.StartSpot;
        boss.Spot = start;
        boss.SnapTo(boss.Arena.PositionOf(start));
        boss.SetPose(boss.Arena.SideOf(start));
    }

    public override void LogicUpdate()
    {
        if (boss.FightStarted)
        {
            ChangeState(EnemyStateId.Engage);
        }
    }

    public override void PhysicsUpdate()
    {
        Movement.Execute(MoveIntent.Stop);
    }
}
