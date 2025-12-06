public class StateMachine
{
    public States CurrentPlayerState { get; set; }

    public void Initialize(States startingState)
    {
        CurrentPlayerState = startingState;
        CurrentPlayerState.Enter();
    }

    public void ChangeState(States newState)
    {
        CurrentPlayerState.Exit();
        CurrentPlayerState = newState;
        CurrentPlayerState.Enter();
    }
}
