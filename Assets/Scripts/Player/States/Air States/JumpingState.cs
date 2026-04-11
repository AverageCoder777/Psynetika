using UnityEngine;

public class JumpingState : AirStates
{
    private bool canDoubleJump;
    private bool doubleJumpInput;
    private float wallContactTime = 0f;

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

        // Применяем гравитационный скейлинг
        if (player.Rb.linearVelocity.y >= 0)
        {
            player.Rb.gravityScale = player.UpGravityScale;
        }
        else
        {
            player.Rb.gravityScale = player.DownGravityScale;
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        
        // Проверяем столкновение со стеной
        bool touchingWall = DetectWall();
        
        if (touchingWall)
        {
            wallContactTime += Time.deltaTime;
            if (wallContactTime >= player.WallWaitTime)
            {
                stateMachine.ChangeState(player.WallState);
                return;
            }
        }
        else
        {
            wallContactTime = 0f;
        }
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
            stateMachine.ChangeState(player.FallingState);
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
