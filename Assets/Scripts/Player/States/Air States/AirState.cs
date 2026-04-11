using UnityEngine;

public class AirState : AirStates
{
    private bool state_from_jump = false; // флаг для отслеживания, пройдет ли персонаж через player.Rb.linearVelocity.y == 0 в верхней точке параболы, чтобы случайно не врубать Idle в воздухе

    public AirState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        if (player.Rb.linearVelocity.y < 0)
        {
            animator.SetTrigger("Falling");
            state_from_jump = false;
            player.Rb.gravityScale = player.DownGravityScale;
        }
        else if (player.Rb.linearVelocity.y > 0)
        {
            animator.SetTrigger("Jumping");
            state_from_jump = true;
            player.Rb.gravityScale = player.UpGravityScale;
        }
        if (player.DebugMessages) Debug.Log("Entered Air State");
    }

    public override void HandleInput()
    {
        base.HandleInput();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (player.Rb.linearVelocity.y == 0 && state_from_jump)
        {
            state_from_jump = false;
        }
        if (player.Rb.linearVelocity.y == 0 && !state_from_jump)
        {
            stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void Exit()
    {
        player.Rb.gravityScale = 1f;
        animator.ResetTrigger("Falling");
        animator.ResetTrigger("Jumping");
        if (player.DebugMessages) Debug.Log("Exited Air State");
    }
}