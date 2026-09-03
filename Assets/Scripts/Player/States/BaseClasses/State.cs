using UnityEngine;
public abstract class State
{
    protected PlayerController player;
    protected PlayerMoving movement;
    protected PlayerAttack attack;
    protected PlayerCharacterManager charManager;
    protected Animator animator;
    protected StateMachine stateMachine;
    protected PlayerStaticSettings settings;
    public State(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
    {
        this.player = player;
        movement = player.Movement;
        attack = player.Attack;
        charManager = player.PlayerCharManager;
        animator = charManager.ActiveAnimator;
        this.stateMachine = stateMachine;
        this.settings = settings;
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
