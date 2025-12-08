public class EnemyStateMachine
{
    public EnemyStates CurrentEnemyState { get; set; }

    public void Initialize(EnemyStates startingState)
    {
        CurrentEnemyState = startingState;
        CurrentEnemyState.Enter();
    }

    public void ChangeState(EnemyStates newState)
    {
        CurrentEnemyState.Exit();
        CurrentEnemyState = newState;
        CurrentEnemyState.Enter();
    }
}
