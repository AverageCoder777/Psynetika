using UnityEngine;

public class PlayerStateMachine
{
    public PlayerState CurrentPlayerState { get; set; }

    public void Initialize(PlayerState startingState)
    {
        CurrentPlayerState = startingState;
        CurrentPlayerState.Enter();
    }

    public void ChangeState(PlayerState newState)
    {
        CurrentPlayerState.Exit();
        CurrentPlayerState = newState;
        CurrentPlayerState.Enter();
    }
}
