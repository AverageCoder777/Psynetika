using UnityEngine;

public class JumpingState : AirStates
{
    private bool canDoubleJump;
    private bool doubleJumpInput;

    public JumpingState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        Jump(player.Thrust);
        canDoubleJump = true;
        animator.SetTrigger("Jumping");
        player.Rb.gravityScale = player.UpGravityScale;
        wallContactTime = 0f;
        if (player.DebugMessages)
        {
            Debug.Log("Entered Jumping State");
        }
    }

    private void Jump(float jumpForce)
    {
        player.Rb.AddForce(player.transform.up * jumpForce, ForceMode2D.Impulse);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
    }

    public override void HandleInput()
    {
        base.HandleInput();
        doubleJumpInput = player.PlayerInput.actions["Jump"].WasPressedThisFrame();

        if (doubleJumpInput && canDoubleJump)
        {
            Jump(player.DoubleJumpThrust);
            animator.SetTrigger("DoubleJumping");
            canDoubleJump = false;
        }
        player.Rb.gravityScale = 1f;

        if (player.Rb.linearVelocity.y < -0.001f)
        {
            stateMachine.ChangeState(player.AirState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        animator.ResetTrigger("Jumping");
        animator.ResetTrigger("DoubleJumping");

        if (player.DebugMessages)
        {
            Debug.Log("Exited Jumping State");
        }
    }
}
