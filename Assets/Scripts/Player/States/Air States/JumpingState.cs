using UnityEngine;

public class JumpingState : AirStates
{
    private static readonly int DoubleJumpingHash = Animator.StringToHash("DoubleJumping");
    private static readonly int JumpingHash = Animator.StringToHash("Jumping");
    private bool canDoubleJump;
    private bool doubleJumpInput;

    public JumpingState(PlayerController player, StateMachine stateMachine)
        : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        Jump(Movement.Thrust);
        canDoubleJump = true;
        Animator.SetTrigger(JumpingHash);
        Movement.Rb.gravityScale = Movement.UpGravityScale;
        wallContactTime = 0f;
        player.LastState = this;
    }

    private void Jump(float jumpForce)
    {
        Movement.Rb.AddForce(Movement.Rb.transform.up * jumpForce, ForceMode2D.Impulse);
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
        doubleJumpInput = Movement.PlayerInput.actions["Jump"].WasPressedThisFrame();

        if (doubleJumpInput && canDoubleJump)
        {
            Jump(Movement.DoubleJumpThrust);
            Animator.SetTrigger(DoubleJumpingHash);
            canDoubleJump = false;
        }
        Movement.Rb.gravityScale = Movement.Rb.linearVelocity.y >= 0 ? Movement.UpGravityScale : Movement.DownGravityScale;

        if (Movement.Rb.linearVelocity.y < -0.001f)
        {
            stateMachine.ChangeState(player.FlyingState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        Animator.ResetTrigger(JumpingHash);
        Animator.ResetTrigger(DoubleJumpingHash);
    }
}
