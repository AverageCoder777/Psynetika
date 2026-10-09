using UnityEngine;

public class WallState : State
{
    private static readonly int WallSlidingHash = Animator.StringToHash("WallSliding");
    private bool jumpInput = false;
    private Vector2 wallSurfaceNormal = Vector2.zero;
    private Collider2D wallCollider;
    private Collider2D lastJumpedWall;
    private float lastWallJumpTime = float.NegativeInfinity;

    public WallState(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
        : base(player, stateMachine, settings) { }

    public override void Enter()
    {
        charManager.ActiveAnimator.SetTrigger("WallSlideBegin");
        charManager.ActiveAnimator.SetBool(WallSlidingHash, true);
        movement.Rb.gravityScale = 1f;
    }

    public bool IsReattachBlocked(Collider2D wall, float duration)
    {
        return wall == lastJumpedWall && Time.time - lastWallJumpTime < duration;
    }

    public override void HandleInput()
    {
        base.HandleInput();
        jumpInput = movement.PlayerInput.actions["Jump"].WasPressedThisFrame();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        RaycastHit2D wallHit = DetectWallHit();
        wallCollider = wallHit.collider;
        wallSurfaceNormal = wallHit.normal;

        if (movement.Rb.linearVelocity.y > settings.detection.jumpWallVelocityThreshold || wallCollider == null)
        {
            stateMachine.ChangeState(player.IdleState);
            return;
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
        lastJumpedWall = wallCollider;
        lastWallJumpTime = Time.time;
        player.Movement.TryConsumeAirJump();
        float horizontalVelocity = wallSurfaceNormal.x * settings.wall.wallJumpSpeed * settings.wall.wallJumpForce;
        float verticalVelocity = Mathf.Sqrt(settings.wall.wallJumpForce * settings.wall.wallVerticalMultiplier);

        movement.Rb.linearVelocity = new Vector2(horizontalVelocity, verticalVelocity);

        charManager.ActiveSR.flipX = !charManager.ActiveSR.flipX; // Через флип происходит и флип луча
    }

    public override void Exit()
    {
        charManager.ActiveAnimator.SetBool(WallSlidingHash, false);
        base.Exit();
    }
}