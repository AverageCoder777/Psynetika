using UnityEngine;

public abstract class AirStates : State
{
    private static readonly int DoubleJumpingHash = Animator.StringToHash("DoubleJumping");

    public AirStates(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
        : base(player, stateMachine, settings) { }
    public override void Enter()
    {
        base.Enter();
    }

    public override void HandleInput()
    {
        base.HandleInput();
        movement.MovementInput = movement.PlayerInput.actions["Move"].ReadValue<Vector2>();
        movement.TryCutJump();
    }

    protected bool TryDoubleJump()
    {
        if (!movement.JumpPressedThisFrame)
        {
            return false;
        }

        // Coyote time: только что сошёл с края — это ещё обычный прыжок с земли, а не двойной.
        if (movement.InCoyoteTime)
        {
            stateMachine.ChangeState(player.JumpingState);
            return true;
        }

        if (!player.Movement.TryConsumeAirJump())
        {
            return false;
        }

        ApplyJumpVelocity();
        charManager.ActiveAnimator.SetTrigger(DoubleJumpingHash);
        return true;
    }

    protected void ApplyJumpVelocity()
    {
        Vector2 velocity = movement.Rb.linearVelocity;
        velocity.y = settings.jump.thrust / movement.Rb.mass;
        movement.Rb.linearVelocity = velocity;
        movement.NotifyJumped(allowJumpCut: true);
    }

    // Приземление: общий выход в Idle для всех воздушных состояний.
    protected void Land()
    {
        player.Movement.ResetAirJumps();
        movement.NotifyLanded();
        stateMachine.ChangeState(player.IdleState);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (movement.Rb.linearVelocity.y <= 0f && DetectFloor()=="Floor")
        {
            Land();
            return;
        }

        if (player.RopeState.TryFindRope())
        {
            stateMachine.ChangeState(player.RopeState);
            return;
        }

        Collider2D wall = DetectWall();
        if (wall != null
            && charManager.GetCurrentCharacterType() != PlayerCharacterType.Satan
            && !player.WallState.IsReattachBlocked(wall, settings.wall.wallWaitTime))
        {
            stateMachine.ChangeState(player.WallState);
            return;
        }
    }
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        float targetVelocityX = movement.MovementInput.x * movement.GetCurrentSpeed() * settings.jump.airSpeedMultiplier;
        float currentVelocityX = movement.Rb.linearVelocity.x;
        float newVelocityX = currentVelocityX;

        // После прыжка с верёвки инерция раскачки сохраняется: управление может тормозить и разворачивать,
        // но не срезает разгон до обычной воздушной скорости.
        bool fromRope = player.PlayerSM.LastPlayerState is RopeState;
        bool carryRopeMomentum = fromRope
            && Mathf.Sign(currentVelocityX) == Mathf.Sign(targetVelocityX)
            && Mathf.Abs(currentVelocityX) > Mathf.Abs(targetVelocityX);

        if (movement.MovementInput.x != 0 && !carryRopeMomentum)
        {
            bool turning = currentVelocityX * targetVelocityX < 0f;
            float rate = turning ? settings.move.turnAccelerationRate : settings.move.accelerationRate;
            newVelocityX = Mathf.Lerp(currentVelocityX, targetVelocityX, rate * Time.fixedDeltaTime);
        }
        else if (movement.MovementInput.x == 0 && !fromRope)
        {
            // Без ввода герой мягко гасит горизонтальную скорость, а не скользит бесконечно.
            newVelocityX = Mathf.Lerp(currentVelocityX, 0f, settings.jump.airFrictionRate * Time.fixedDeltaTime);
        }
        movement.Rb.linearVelocity = new Vector2(newVelocityX, movement.Rb.linearVelocity.y);

        if (movement.MovementInput.x > 0.01f)
            charManager.ActiveSR.flipX = false;
        else if (movement.MovementInput.x < -0.01f)
            charManager.ActiveSR.flipX = true;

        ApplyAirGravity();
    }

    // Гравитация в воздухе: сильнее при падении, слабее у вершины при зажатом прыжке, падение ограничено.
    protected void ApplyAirGravity()
    {
        Vector2 velocity = movement.Rb.linearVelocity;
        float gravity = velocity.y > 0f ? settings.jump.upGravityScale : settings.jump.downGravityScale;
        if (Mathf.Abs(velocity.y) < settings.jump.apexHangSpeed && movement.JumpHeld)
        {
            gravity *= settings.jump.apexGravityMultiplier;
        }
        movement.Rb.gravityScale = gravity;

        if (velocity.y < -settings.jump.maxFallSpeed)
        {
            velocity.y = -settings.jump.maxFallSpeed;
            movement.Rb.linearVelocity = velocity;
        }
        movement.TrackFall(velocity.y);
    }

    public override void Exit()
    {
        charManager.ActiveAnimator.ResetTrigger(DoubleJumpingHash);
        base.Exit();
    }
}
