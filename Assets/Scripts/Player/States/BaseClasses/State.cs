using UnityEngine;
public abstract class State
{
    protected Player player;
    protected StateMovMachine stateMachine;
    protected Animator animator => player.ActiveAnimator;
    public State(Player player, StateMovMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;
    }
    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void LogicUpdate()
    {
    }
    public virtual void HandleInput()
    {
    }
    public virtual void PhysicsUpdate()
    {
    }
}
