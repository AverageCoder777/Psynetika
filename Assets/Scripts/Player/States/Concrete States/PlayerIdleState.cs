using UnityEngine;

public class PlayerIdleState : PlayerGroundedState
{
    private bool jump;
    private bool crouch;
    private bool rollRequest;
    public PlayerIdleState(Player player, PlayerStateMachine playerStateMachine)
        : base(player, playerStateMachine)
    {
    }
    public override void Enter()
    {
        base.Enter();
        animator.SetBool("Moving", false);
        player.Rb.linearVelocity = new Vector2(0, player.Rb.linearVelocity.y);
        Debug.Log("Entered Idle State");

    }
    public override void HandleInput()
    {
        base.HandleInput();
        crouch = player.playerInput.actions["Crouch"].IsPressed();
        jump = player.playerInput.actions["Jump"].WasPressedThisFrame();
        rollRequest = player.playerInput.actions["Roll"].WasPressedThisFrame();
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
            stateMachine.ChangeState(player.JumpingState);
        }
        if (player.Rb.linearVelocity.y < 0)
        {
            stateMachine.ChangeState(player.FallingState);
        }
        if (rollRequest)
        {
            stateMachine.ChangeState(player.RollingState);
        }
    }
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }


}