using UnityEngine;

public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(Player player, PlayerStateMachine playerStateMachine) : base(player, playerStateMachine)
    {
    }
    public override void Enter()
    {
        player.MovementInput = Vector2.zero;
        animator.SetBool("Grounded", true);
    }
    public override void HandleInput()
    {
        base.HandleInput();     
    }
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
    public override void Exit()
    {
        base.Exit();
        animator.SetBool("Moving", false);
        animator.SetBool("Grounded", false);
    }
}
