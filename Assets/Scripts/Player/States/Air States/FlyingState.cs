using UnityEngine;

public class FlyingState : AirStates
{
    private static readonly int JumpingHash = Animator.StringToHash("Jumping");

    public FlyingState(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
        : base(player, stateMachine, settings) { }

    public override void Enter()
    {
        if (movement.Rb.linearVelocity.y < 0)
        {
            movement.Rb.gravityScale = settings.jump.downGravityScale;
        }
        else if (movement.Rb.linearVelocity.y > 0)
        {
            animator.SetTrigger(JumpingHash);
            movement.Rb.gravityScale = settings.jump.upGravityScale;
        }
    }

    public override void HandleInput()
    {
        if (player.LastState is not WallState)
        {
            base.HandleInput();
        }
    }

    public override void PhysicsUpdate()
    {
        if (player.LastState is not WallState)
        {
            base.PhysicsUpdate();
        }
        else
        {
            if (movement.Rb.linearVelocity.y > 0)
            {
                movement.Rb.gravityScale = settings.jump.upGravityScale;
            }
            else
            {
                movement.Rb.gravityScale = settings.jump.downGravityScale;
            }
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (DetectFloor() != "None")
        {
            player.LastState = this;
            stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void Exit()
    {
        movement.Rb.gravityScale = movement.Rb.linearVelocity.y >= 0f ? settings.jump.upGravityScale : settings.jump.downGravityScale;
        animator.ResetTrigger(JumpingHash);
    }
}