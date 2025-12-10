using UnityEngine;
public abstract class States
{
    protected Player player;
    protected StateMachine stateMachine;
    protected Animator animator => player.ActiveAnimator;
    public States(Player player, StateMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;
    }
    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void LogicUpdate()
    {
        if (player.MovementInput.x != 0)
        {
            animator.SetBool("Moving", true);
        }
        else
        {
            animator.SetBool("Moving", false);
        }
    }
    public virtual void HandleInput()
    {
        player.MovementInput = player.PlayerInput.actions["Move"].ReadValue<Vector2>();
    }
    public virtual void PhysicsUpdate()
    {
        player.Rb.linearVelocity = new Vector2(player.MovementInput.x * player.Speed, player.Rb.linearVelocity.y);
        if (player.MovementInput.x > 0.01f)
            player.ActiveSR.flipX = false;
        else if (player.MovementInput.x < -0.01f)
            player.ActiveSR.flipX = true;
    }
    public virtual void AnimationTriggerEvent(Player.AnimationTriggerType triggerType) { }
}
