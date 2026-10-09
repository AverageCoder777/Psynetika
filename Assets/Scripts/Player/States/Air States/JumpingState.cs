using UnityEngine;

public class JumpingState : AirStates
{
    private static readonly int JumpingHash = Animator.StringToHash("Jumping");

    public JumpingState(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
        : base(player, stateMachine, settings)
    {
    }

    public override void Enter()
    {
        player.TryConsumeAirJump();
        ApplyJumpVelocity();
        charManager.ActiveAnimator.SetTrigger(JumpingHash);
        movement.Rb.gravityScale = settings.jump.upGravityScale;
        player.LastState = this;
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        movement.Rb.gravityScale = movement.Rb.linearVelocity.y >= 0 ? settings.jump.upGravityScale : settings.jump.downGravityScale;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (stateMachine.CurrentPlayerState != this)
        {
            return;
        }

        if (movement.Rb.linearVelocity.y < settings.detection.jumpWallVelocityThreshold)
        {
            stateMachine.ChangeState(player.FlyingState);
        }
    }

    public override void HandleInput()
    {
        base.HandleInput();
        TryDoubleJump();
    }

    public override void Exit()
    {
        base.Exit();
        charManager.ActiveAnimator.ResetTrigger(JumpingHash);
    }
}
