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

    public RollingState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();
        rollElapsed = 0f;
        rollDir = CharManager.ActiveSR != null && CharManager.ActiveSR.flipX ? -1f : 1f;
        Animator.SetTrigger(RollingHash);
        Animator.SetBool(GroundedHash, true);
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        player.LastState = this;
    }
    public override void LogicUpdate()
    {
        if (rollEnd)
        {
            if (Mathf.Abs(Movement.Rb.linearVelocity.y) < 0.001f)
                stateMachine.ChangeState(player.IdleState);
            else if (Movement.Rb.linearVelocity.y < 0f)
            {
                Animator.SetBool(GroundedHash, false);
                stateMachine.ChangeState(player.FlyingState);
            }
        }
    }
    public override void PhysicsUpdate()
    {
        if (!rollEnd)
        {
            float duration = Movement.RollDuration > 0f ? Movement.RollDuration : 0.0001f;
            float rollSpeed = Movement.RollDistance / duration;
            Movement.Rb.linearVelocity = new Vector2(rollDir * rollSpeed, Movement.Rb.linearVelocity.y);

            rollElapsed += Time.fixedDeltaTime;
            if (rollElapsed >= duration)
            {
                rollEnd = true;
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
                Movement.Rb.linearVelocity = new Vector2(0f, Movement.Rb.linearVelocity.y);
            }
        }
    }
    public override void Exit()
    {
        base.Exit();
        Animator.ResetTrigger(RollingHash);
        rollEnd = false;
    }
}