public class EnemyStateMachine
{
    public EnemyStates CurrentEnemyState { get; private set; }

    public void Initialize(EnemyStates startingState)
    {
        if (startingState == null) return;

        CurrentEnemyState = startingState;
        CurrentEnemyState.Enter();
    }

    // Переходы идут через EnemyController.ChangeState(EnemyStateId) — там же живёт реестр ролей.
    public void ChangeState(EnemyStates newState)
    {
        if (newState == null) return;

        CurrentEnemyState?.Exit();
        CurrentEnemyState = newState;
        CurrentEnemyState.Enter();
    }
}
