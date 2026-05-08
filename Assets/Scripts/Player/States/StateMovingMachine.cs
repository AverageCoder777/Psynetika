using UnityEngine;

public class StateMovMachine
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
        #if UNITY_EDITOR
        Debug.Log("Exited " + CurrentPlayerState + " and entered " + newState);
        #endif
        CurrentPlayerState = newState;
        CurrentPlayerState.Enter();
    }

}
