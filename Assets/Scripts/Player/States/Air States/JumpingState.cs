using UnityEngine;

public class JumpingState : AirStates
{
    private static readonly int DoubleJumpingHash = Animator.StringToHash("DoubleJumping");
    private static readonly int JumpingHash = Animator.StringToHash("Jumping");
    private bool canDoubleJump;
    private bool doubleJumpInput;

    public JumpingState(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
        : base(player, stateMachine, settings)
    {
    }

    public override void Enter()
    {
        Jump(settings.jump.thrust);
        canDoubleJump = true;
        animator.SetTrigger(JumpingHash);
        movement.Rb.gravityScale = settings.jump.upGravityScale;
        wallContactTime = 0f;
        player.LastState = this;
    }

    private void Jump(float jumpForce)
    {
        movement.Rb.AddForce(movement.Rb.transform.up * jumpForce, ForceMode2D.Impulse);
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
        doubleJumpInput = movement.PlayerInput.actions["Jump"].WasPressedThisFrame();

        if (doubleJumpInput && canDoubleJump)
        {
            Jump(settings.jump.doubleJumpThrust);
            animator.SetTrigger(DoubleJumpingHash);
            canDoubleJump = false;
        }
        movement.Rb.gravityScale = movement.Rb.linearVelocity.y >= 0 ? settings.jump.upGravityScale : settings.jump.downGravityScale;

        if (movement.Rb.linearVelocity.y < -0.001f)
        {
            stateMachine.ChangeState(player.FlyingState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        animator.ResetTrigger(JumpingHash);
        animator.ResetTrigger(DoubleJumpingHash);
    }
}
