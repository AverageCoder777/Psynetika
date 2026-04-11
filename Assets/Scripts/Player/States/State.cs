using UnityEngine;
public abstract class State
{
    protected Player player;
    protected StateMachine stateMachine;
    protected Animator animator => player.ActiveAnimator;
    public State(Player player, StateMachine stateMachine)
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
    public virtual void AnimationTriggerEvent(Player.AnimationTriggerType triggerType) { }
}
