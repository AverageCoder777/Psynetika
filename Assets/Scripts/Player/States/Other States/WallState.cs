using UnityEngine;

public class WallState : State
{
    private static readonly int WallSlidingHash = Animator.StringToHash("WallSliding");
    private bool jumpInput = false;
    private Vector2 wallSurfaceNormal = Vector2.zero;

    public WallState(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
        : base(player, stateMachine, settings) { }

    public override void Enter()
    {
        
        animator.SetBool(WallSlidingHash, true);
        movement.Rb.gravityScale = 1f;
        player.LastState = this;
    }

    public override void HandleInput()
    {
        base.HandleInput();
        jumpInput = movement.PlayerInput.actions["Jump"].WasPressedThisFrame();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        DetectWall();

        if (Mathf.Abs(movement.Rb.linearVelocity.y) < 0.001 || wallSurfaceNormal == Vector2.zero)
        {
            stateMachine.ChangeState(player.IdleState);
        }

        if (jumpInput)
        {
            WallJump();
            stateMachine.ChangeState(player.FlyingState);
        }
    }

    public override void PhysicsUpdate()
    {
        float slideVelocity = -settings.wall.wallSlideSpeed;
        movement.Rb.linearVelocity = new Vector2(0, slideVelocity);
    }

    private void WallJump()
    {
        float horizontalVelocity = wallSurfaceNormal.x * settings.wall.wallJumpSpeed * settings.wall.wallJumpForce;
        float verticalVelocity = Mathf.Sqrt(settings.wall.wallJumpForce * 2f);

        movement.Rb.linearVelocity = new Vector2(horizontalVelocity, verticalVelocity);

        charManager.ActiveSR.flipX = !charManager.ActiveSR.flipX;
    }

    public override void Exit()
    {
        animator.SetBool(WallSlidingHash, false);
        base.Exit();
    }

    private void DetectWall()
    {
        Vector2 wallDetectionDirection = charManager.ActiveSR.flipX ? Vector2.left : Vector2.right;
        Vector2 raycastOrigin = (Vector2)player.transform.position + wallDetectionDirection * 0.25f;

        RaycastHit2D hit = Physics2D.Raycast(
            raycastOrigin,
            wallDetectionDirection,
            settings.wall.wallDetectionDistance,
            LayerMask.GetMask("Walls")
        );
        if (player.debugMessages)
        {
            Debug.DrawRay(raycastOrigin, wallDetectionDirection * settings.wall.wallDetectionDistance,
                hit.collider != null ? Color.green : Color.red);
        }
        if (hit.collider != null)
        {
            wallSurfaceNormal = hit.normal;
        }
        else
        {
            wallSurfaceNormal = Vector2.zero;
        }
    }
}
