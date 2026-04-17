public class StateCharMachine
{
    public PlayerState CurrentPlayerState { get; set; }

    public void Initialize(PlayerState startingState)
    {
        CurrentPlayerState = startingState;
        CurrentPlayerState.Enter();
    }
    public PlayerState GetCurrentState()
    {
        return CurrentPlayerState;
    }

    public void ChangeState(PlayerState newState)
    {
        CurrentPlayerState.Exit();
        CurrentPlayerState = newState;
        CurrentPlayerState.Enter();
    }
}
