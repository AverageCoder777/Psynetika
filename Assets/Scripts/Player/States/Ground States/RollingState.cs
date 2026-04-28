using UnityEngine;
public class RollingState : GroundedStates
{
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int RollingHash = Animator.StringToHash("Rolling");
    private float rollElapsed = 0f;
    private float rollDir = 1f;
    private bool rollEnd = false;
    readonly int playerLayer = LayerMask.NameToLayer("Player");
    readonly int enemyLayer = LayerMask.NameToLayer("Enemy");

    public RollingState(Player player, StateMovMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();
        rollElapsed = 0f;
        rollDir = player.ActiveSR != null && player.ActiveSR.flipX ? -1f : 1f;
        Animator.SetTrigger(RollingHash);
        Animator.SetBool(GroundedHash, true);
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        if (player.DebugMessages) Debug.Log("Entered Rolling State");
        player.LastState = this;
    }
    public override void LogicUpdate()
    {
        if (rollEnd)
        {
            if (player.Rb.linearVelocity.y == 0f)
                stateMachine.ChangeState(player.IdleState);
            else if (player.Rb.linearVelocity.y < 0f)
            {
                Animator.SetBool(GroundedHash, false);
                stateMachine.ChangeState(player.AirState);
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
        Animator.ResetTrigger(RollingHash);
        rollEnd = false;
        if (player.DebugMessages) Debug.Log("Exited Rolling State");
    }
}