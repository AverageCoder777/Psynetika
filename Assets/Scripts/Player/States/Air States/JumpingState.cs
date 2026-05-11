using UnityEngine;

public class JumpingState : AirStates
{
    private static readonly int DoubleJumpingHash = Animator.StringToHash("DoubleJumping");
    private static readonly int JumpingHash = Animator.StringToHash("Jumping");
    private bool canDoubleJump;
    private bool doubleJumpInput;

    public JumpingState(Player player, StateMovMachine stateMachine)
        : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        Jump(player.Thrust);
        canDoubleJump = true;
        Animator.SetTrigger(JumpingHash);
        player.Rb.gravityScale = player.UpGravityScale;
        wallContactTime = 0f;
        player.LastState = this;
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
            Animator.SetTrigger(DoubleJumpingHash);
            canDoubleJump = false;
        }
        player.Rb.gravityScale = player.Rb.linearVelocity.y >= 0 ? player.UpGravityScale : player.DownGravityScale;

        if (player.Rb.linearVelocity.y < -0.001f)
        {
            stateMachine.ChangeState(player.AirState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        Animator.ResetTrigger(JumpingHash);
        Animator.ResetTrigger(DoubleJumpingHash);
    }
}
