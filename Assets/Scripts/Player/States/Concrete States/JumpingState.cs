using UnityEngine;

public class JumpingState : States
{
    private bool canDoubleJump;
    private bool doubleJumpInput;
    private float wallCheckDistance = 0.5f;

    public JumpingState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        Jump();
        canDoubleJump = true;
        animator.SetTrigger("Jumping");
        if (player.DebugMessages)
        {
            Debug.Log("Entered Jumping State");
        }
    }

    private void Jump()
    {
        player.Rb.AddForce(player.transform.up * player.Thrust, ForceMode2D.Impulse);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public override void HandleInput()
    {
        base.HandleInput();
        doubleJumpInput = player.PlayerInput.actions["Jump"].WasPressedThisFrame();

        if (
            doubleJumpInput
            && canDoubleJump
            && player.transform.position.y < player.MaxDoubleJumpHeight
        )
        {
            Jump();
            animator.SetTrigger("DoubleJumping");
            canDoubleJump = false;
        }

        if (player.Rb.linearVelocity.y < -0.001f)
        {
            stateMachine.ChangeState(player.FallingState);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if collision is with Walls layer
        if (collision.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            stateMachine.ChangeState(player.WallState);
        }
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Keep in WallState while touching walls
        if (collision.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            stateMachine.ChangeState(player.WallState);
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
