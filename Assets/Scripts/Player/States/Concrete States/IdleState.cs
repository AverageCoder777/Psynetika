using UnityEngine;

public class IdleState : GroundedState
{
    private bool jump;
    private bool crouch;
    private bool roll;
    private bool @switch;
    private bool hit;
    public IdleState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {
    }
    public override void Enter()
    {
        base.Enter();
        animator.SetBool("Moving", false);
        grounded = true;
        player.Rb.linearVelocity = new Vector2(0, player.Rb.linearVelocity.y);
        Debug.Log("Entered Idle State");

    }
    public override void HandleInput()
    {
        base.HandleInput();
        crouch = player.playerInput.actions["Crouch"].IsPressed();
        jump = player.playerInput.actions["Jump"].WasPressedThisFrame();
        roll = player.playerInput.actions["Roll"].WasPressedThisFrame();
        @switch = player.playerInput.actions["Switch"].WasPressedThisFrame();
        hit = player.playerInput.actions["Attack"].WasPressedThisFrame();
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (crouch)
        {
            stateMachine.ChangeState(player.CrouchingState);
        }
        else if (jump)
        {
            grounded = false;
            stateMachine.ChangeState(player.JumpingState);
        }
        if (player.Rb.linearVelocity.y < 0)
        {
            grounded = false;
            stateMachine.ChangeState(player.FallingState);
        }
        if (roll)
        {
            stateMachine.ChangeState(player.RollingState);
        }
        if (@switch)
        {
            stateMachine.ChangeState(player.SwitchState);
        }
        if (hit)
        {
            stateMachine.ChangeState(player.HittingState);
        }
    }
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
    public override void Exit()
    {
        base.Exit();
        Debug.Log("Exited Idle State");
    }
}