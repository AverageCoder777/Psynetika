using UnityEngine;

public abstract class PlayerState
{
    protected Player player;
    protected PlayerStateMachine stateMachine;
    protected Animator animator;
    public PlayerState(Player player, PlayerStateMachine playerStateMachine)
    {
        this.player = player;
        this.stateMachine = playerStateMachine;
        animator = player.GetActiveAnimator();
    }
    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void LogicUpdate() { }
    public virtual void HandleInput()
    {
        player.MovementInput = player.playerInput.actions["Move"].ReadValue<Vector2>();
    }
    public virtual void PhysicsUpdate()
    {
        player.Rb.linearVelocity = new Vector2(player.MovementInput.x * player.Speed, player.Rb.linearVelocity.y);
        if (player.MovementInput.x > 0.01f)
            player.GetActiveSpriteRenderer().flipX = false;
        else if (player.MovementInput.x < -0.01f)
            player.GetActiveSpriteRenderer().flipX = true;
    }
    public virtual void AnimationTriggerEvent(Player.AnimationTriggerType triggerType) { }
}
