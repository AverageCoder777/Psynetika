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
    }

    protected bool TryDoubleJump()
    {
        if (!movement.PlayerInput.actions["Jump"].WasPressedThisFrame() || !player.Movement.TryConsumeAirJump())
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
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (movement.Rb.linearVelocity.y <= 0f && DetectFloor()=="Floor")
        {
            player.Movement.ResetAirJumps();
            stateMachine.ChangeState(player.IdleState);
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
        bool carryRopeMomentum = player.LastState is RopeState
            && Mathf.Sign(currentVelocityX) == Mathf.Sign(targetVelocityX)
            && Mathf.Abs(currentVelocityX) > Mathf.Abs(targetVelocityX);

        if (movement.MovementInput.x != 0 && !carryRopeMomentum)
        {
            newVelocityX = Mathf.Lerp(
                currentVelocityX,
                targetVelocityX,
                settings.move.accelerationRate * Time.fixedDeltaTime);
        }
        else if (Mathf.Abs(currentVelocityX) < 0.01f)
        {
            newVelocityX = currentVelocityX;
        }
        movement.Rb.linearVelocity = new Vector2(newVelocityX, movement.Rb.linearVelocity.y);

        if (movement.MovementInput.x > 0.01f)
            charManager.ActiveSR.flipX = false;
        else if (movement.MovementInput.x < -0.01f)
            charManager.ActiveSR.flipX = true;
        if (movement.Rb.linearVelocity.y > 0)
        {
            movement.Rb.gravityScale = settings.jump.upGravityScale;
        }
        else
        {
            movement.Rb.gravityScale = settings.jump.downGravityScale;
        }
    }
    public override void Exit()
    {
        charManager.ActiveAnimator.ResetTrigger(DoubleJumpingHash);
        base.Exit();
    }
}
