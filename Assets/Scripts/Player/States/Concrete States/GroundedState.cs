using UnityEngine;

public class GroundedState : States
{
    protected bool grounded = true;
    public GroundedState(Player player, StateMachine stateMachine) : base(player, stateMachine)
    {
    }
    public override void Enter()
    {
        player.MovementInput = Vector2.zero;
        animator.SetBool("Grounded", true);
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
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
