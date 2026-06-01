using UnityEngine;

public class WallState : State
{
    private static readonly int WallSlidingHash = Animator.StringToHash("WallSliding");
    private bool jumpInput = false;
    private readonly float wallJumpThrustFactor = 1f;
    private readonly float wallJumpSpeed = 5f;
    private Vector2 wallSurfaceNormal = Vector2.zero;

    public WallState(PlayerController player, StateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        Animator.SetBool(WallSlidingHash, true);
        Movement.Rb.gravityScale = 1f;
        player.LastState = this;
    }

    public override void HandleInput()
    {
        base.HandleInput();
        jumpInput = Movement.PlayerInput.actions["Jump"].WasPressedThisFrame();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        DetectWall();

        if (Mathf.Abs(Movement.Rb.linearVelocity.y) < 0.001 || wallSurfaceNormal == Vector2.zero)
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
        float slideVelocity = -Movement.WallSlideSpeed;
        Movement.Rb.linearVelocity = new Vector2(0, slideVelocity);
    }

    private void WallJump()
    {
        float horizontalVelocity = wallSurfaceNormal.x * wallJumpSpeed * wallJumpThrustFactor;//5 - базовая скорость отталкивания
        float verticalVelocity = Mathf.Sqrt(Movement.WallJumpForce * 2f);

        Movement.Rb.linearVelocity = new Vector2(horizontalVelocity, verticalVelocity);

        CharManager.ActiveSR.flipX = !CharManager.ActiveSR.flipX;
    }

    public override void Exit()
    {
        Animator.SetBool(WallSlidingHash, false);
        base.Exit();
    }

    private void DetectWall()
    {
        Vector2 wallDetectionDirection = CharManager.ActiveSR.flipX ? Vector2.left : Vector2.right;
        Vector2 raycastOrigin = (Vector2)player.transform.position + wallDetectionDirection * 0.25f;

        RaycastHit2D hit = Physics2D.Raycast(
            raycastOrigin,
            wallDetectionDirection,
            Movement.WallDetectionDistance,
            LayerMask.GetMask("Walls")
        );
#if UNITY_EDITOR
        if (player.debugMessages)
        {
            Debug.DrawRay(raycastOrigin, wallDetectionDirection * Movement.WallDetectionDistance,
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
