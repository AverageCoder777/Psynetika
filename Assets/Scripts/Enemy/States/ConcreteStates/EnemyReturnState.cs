// Возвращение к точке спавна после потери игрока (EnemyConfig.home.returnWhenLost).
// Заметив игрока по дороге, сразу реагирует на него.
public class EnemyReturnState : EnemyStates
{
    // LastMoveBlocked до первого своего шага остался от прошлого состояния (например, погоня упёрлась в обрыв).
    private bool stepped;

    public EnemyReturnState(EnemyController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    public override void Enter()
    {
        stepped = false;
    }

    public override void LogicUpdate()
    {
        if (TryReactToPlayer()) return;

        // Дома или дорогу перекрыл обрыв/стена (например, враг упал ниже): покой там, где стоит.
        if (Movement.IsAt(Movement.SpawnPosition) || (stepped && Movement.LastMoveBlocked))
        {
            ChangeState(EnemyStateId.Rest);
            return;
        }

        UpdateLocomotionFlags();
    }

    public override void PhysicsUpdate()
    {
        Movement.Execute(MoveIntent.To(Movement.SpawnPosition));
        stepped = true;
    }

    public override void Exit()
    {
        ClearLocomotionFlags();
    }
}
