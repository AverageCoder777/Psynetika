using UnityEngine;

public class FlyingState : AirStates
{
    private static readonly int JumpingHash = Animator.StringToHash("Jumping");

    public FlyingState(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
        : base(player, stateMachine, settings) { }

    // Сразу после отскока от стены ввод ненадолго отключён, чтобы зажатое «к стене» не гасило отскок.
    // Потом возвращаются управление в воздухе и двойной прыжок.
    private bool WallJumpLocked => player.PlayerSM.LastPlayerState is WallState
        && Time.time - player.WallState.LastWallJumpTime < settings.wall.wallJumpControlLock;

    public override void Enter()
    {
        if (movement.Rb.linearVelocity.y < 0)
        {
            movement.Rb.gravityScale = settings.jump.downGravityScale;
        }
        else if (movement.Rb.linearVelocity.y > 0)
        {
            charManager.ActiveAnimator.SetTrigger(JumpingHash);
            movement.Rb.gravityScale = settings.jump.upGravityScale;
        }
    }

    public override void HandleInput()
    {
        if (!WallJumpLocked)
        {
            base.HandleInput();
            TryDoubleJump();
        }
    }

    public override void PhysicsUpdate()
    {
        if (!WallJumpLocked)
        {
            base.PhysicsUpdate();
        }
        else
        {
            ApplyAirGravity();
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (stateMachine.CurrentPlayerState != this)
        {
            return;
        }

        if (DetectFloor() != "None")
        {
            if (movement.Rb.linearVelocity.y > 0f)
            {
                return;
            }

            Land();
        }
    }

    public override void Exit()
    {
        movement.Rb.gravityScale = movement.Rb.linearVelocity.y >= 0f ? settings.jump.upGravityScale : settings.jump.downGravityScale;
        charManager.ActiveAnimator.ResetTrigger(JumpingHash);
    }
}
