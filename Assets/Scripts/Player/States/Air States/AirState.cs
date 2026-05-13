using UnityEngine;

public class AirState : AirStates
{
    private static readonly int JumpingHash = Animator.StringToHash("Jumping");
    private static readonly int FallingHash = Animator.StringToHash("Falling");

    public AirState(Player player, StateMovMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        if (player.Rb.linearVelocity.y < 0)
        {
            Animator.SetTrigger(FallingHash);
            player.Rb.gravityScale = player.DownGravityScale;
        }
        else if (player.Rb.linearVelocity.y > 0)
        {
            Animator.SetTrigger(JumpingHash);
            player.Rb.gravityScale = player.UpGravityScale;
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
            if (player.Rb.linearVelocity.y > 0)
            {
                player.Rb.gravityScale = player.UpGravityScale;
            }
            else
            {
                player.Rb.gravityScale = player.DownGravityScale;
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
        player.Rb.gravityScale = player.Rb.linearVelocity.y >= 0f ? player.UpGravityScale : player.DownGravityScale;
        Animator.ResetTrigger(FallingHash);
        Animator.ResetTrigger(JumpingHash);
    }
}