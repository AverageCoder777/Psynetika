using UnityEngine;
using System.Collections;

public class RollingState : States
{
    private float rollElapsed = 0f;
    private float rollDir = 1f;
    private bool rollEnd = false;
    int playerLayer = LayerMask.NameToLayer("Player");
    int enemyLayer = LayerMask.NameToLayer("Enemy");

    public RollingState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();
        rollElapsed = 0f;
        rollDir = player.ActiveSR != null && player.ActiveSR.flipX ? -1f : 1f;
        animator.SetTrigger("Rolling");
        animator.SetBool("Grounded", true);
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        if (player.DebugMessages) Debug.Log("Entered Rolling State");
    }
    public override void LogicUpdate()
    {
        if (rollEnd)
        {
            if (player.Rb.linearVelocity.y == 0f)
                stateMachine.ChangeState(player.IdleState);
            else if (player.Rb.linearVelocity.y < 0f)
            {
                animator.SetBool("Grounded", false);
                stateMachine.ChangeState(player.FallingState);
            }
        }
    }
    public override void PhysicsUpdate()
    {
        if (!rollEnd)
        {
            float duration = player.RollDuration > 0f ? player.RollDuration : 0.0001f;
            float rollSpeed = player.RollDistance / duration;
            player.Rb.linearVelocity = new Vector2(rollDir * rollSpeed, player.Rb.linearVelocity.y);

            rollElapsed += Time.fixedDeltaTime;
            if (rollElapsed >= duration)
            {
                rollEnd = true;
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
                player.Rb.linearVelocity = new Vector2(0f, player.Rb.linearVelocity.y);
            }
        }
    }
    public override void Exit()
    {
        base.Exit();
        animator.ResetTrigger("Rolling");
        rollEnd = false;
        if (player.DebugMessages) Debug.Log("Exited Rolling State");
    }
}