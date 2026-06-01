using UnityEngine;
public abstract class State
{
    protected PlayerController player;
    protected PlayerMoving Movement => player.Movement;
    protected PlayerAttack Attack => player.Attack;
    protected PlayerCharacterManager CharManager => player.PlayerCharManager;
    protected Animator Animator => CharManager.ActiveAnimator;
    protected StateMachine stateMachine;
    public State(PlayerController player, StateMachine stateMachine)
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
