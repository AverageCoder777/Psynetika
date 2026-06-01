using UnityEngine;

public class FlyingState : AirStates
{
    private static readonly int JumpingHash = Animator.StringToHash("Jumping");

    public FlyingState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        if (Movement.Rb.linearVelocity.y < 0)
        {
            Movement.Rb.gravityScale = Movement.DownGravityScale;
        }
        else if (Movement.Rb.linearVelocity.y > 0)
        {
            Animator.SetTrigger(JumpingHash);
            Movement.Rb.gravityScale = Movement.UpGravityScale;
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
            if (Movement.Rb.linearVelocity.y > 0)
            {
                Movement.Rb.gravityScale = Movement.UpGravityScale;
            }
            else
            {
                Movement.Rb.gravityScale = Movement.DownGravityScale;
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
        Movement.Rb.gravityScale = Movement.Rb.linearVelocity.y >= 0f ? Movement.UpGravityScale : Movement.DownGravityScale;
        Animator.ResetTrigger(JumpingHash);
    }
}