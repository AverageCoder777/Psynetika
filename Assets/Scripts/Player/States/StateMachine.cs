public class StateMachine
{
    public State CurrentPlayerState { get; set; }

    public void Initialize(State startingState)
    {
        CurrentPlayerState = startingState;
        CurrentPlayerState.Enter();
    }

    public void ChangeState(State newState)
    {
        CurrentPlayerState.Exit();
        CurrentPlayerState = newState;
        CurrentPlayerState.Enter();
    }
}
