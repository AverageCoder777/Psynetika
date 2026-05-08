using UnityEngine;

public class AirState : AirStates
{
    private static readonly int JumpingHash = Animator.StringToHash("Jumping");
    private static readonly int FallingHash = Animator.StringToHash("Falling");
    private bool state_from_jump = false; // флаг для отслеживания, пройдет ли персонаж через player.Rb.linearVelocity.y == 0 в верхней точке параболы, чтобы случайно не врубать Idle в воздухе

    public AirState(Player player, StateMovMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        if (player.Rb.linearVelocity.y < 0)
        {
            Animator.SetTrigger(FallingHash);
            state_from_jump = false;
            player.Rb.gravityScale = player.DownGravityScale;
        }
        else if (player.Rb.linearVelocity.y > 0)
        {
            Animator.SetTrigger(JumpingHash);
            state_from_jump = true;
            player.Rb.gravityScale = player.UpGravityScale;
        }
#if UNITY_EDITOR
        if (player.DebugMessages) Debug.Log("Entered Air State");
#endif
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

        if (DetectFloor() && state_from_jump)
        {
            state_from_jump = false;
        }
        if (DetectFloor() && !state_from_jump)
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
#if UNITY_EDITOR
        if (player.DebugMessages) Debug.Log("Exited Air State");
#endif
    }
}