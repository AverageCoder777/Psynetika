using UnityEngine;

public class WallState : State
{
    private static readonly int WallSlidingHash = Animator.StringToHash("WallSliding");
    private bool jumpInput = false;
    private readonly float wallJumpThrustFactor = 1f;
    private readonly float wallJumpSpeed = 5f;
    private Vector2 wallSurfaceNormal = Vector2.zero;

    public WallState(Player player, StateMovMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        Animator.SetBool(WallSlidingHash, true);
        player.Rb.gravityScale = 1f;
        player.LastState = this;
    }

    public override void HandleInput()
    {
        base.HandleInput();
        jumpInput = player.PlayerInput.actions["Jump"].WasPressedThisFrame();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        DetectWall();

        if (Mathf.Abs(player.Rb.linearVelocity.y) < 0.001 || wallSurfaceNormal == Vector2.zero)
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
        float slideVelocity = -player.WallSlideSpeed;
        player.Rb.linearVelocity = new Vector2(0, slideVelocity);
    }

    private void WallJump()
    {
        float horizontalVelocity = wallSurfaceNormal.x * wallJumpSpeed * wallJumpThrustFactor;//5 - базовая скорость отталкивания
        float verticalVelocity = Mathf.Sqrt(player.WallJumpForce * 2f);

        player.Rb.linearVelocity = new Vector2(horizontalVelocity, verticalVelocity);

        player.ActiveSR.flipX = !player.ActiveSR.flipX;
    }

    public override void Exit()
    {
        Animator.SetBool(WallSlidingHash, false);
        base.Exit();
    }

    private void DetectWall()
    {
        Vector2 wallDetectionDirection = player.ActiveSR.flipX ? Vector2.left : Vector2.right;
        Vector2 raycastOrigin = (Vector2)player.transform.position + wallDetectionDirection * 0.25f;

        RaycastHit2D hit = Physics2D.Raycast(
            raycastOrigin,
            wallDetectionDirection,
            player.WallDetectionDistance,
            LayerMask.GetMask("Walls")
        );
#if UNITY_EDITOR
        if (player.DebugMessages)
        {
            Debug.DrawRay(raycastOrigin, wallDetectionDirection * player.WallDetectionDistance,
                hit.collider != null ? Color.green : Color.red);
        }
#endif
        if (hit.collider != null)
        {
            // Сохраняем нормаль поверхности стены
            wallSurfaceNormal = hit.normal;
        }
        else
        {
            wallSurfaceNormal = Vector2.zero;
        }
    }
}
