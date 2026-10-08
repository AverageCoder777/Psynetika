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
        if (!movement.PlayerInput.actions["Jump"].WasPressedThisFrame() || !player.TryConsumeAirJump())
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
            player.ResetAirJumps();
            stateMachine.ChangeState(player.IdleState);
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

        if (movement.MovementInput.x != 0)
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
