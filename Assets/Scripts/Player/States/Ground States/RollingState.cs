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

    public RollingState(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
         : base(player, stateMachine, settings) { }

    public override void Enter()
    {
        base.Enter();
        rollElapsed = 0f;
        rollDir = charManager.ActiveSR != null && charManager.ActiveSR.flipX ? -1f : 1f;
        animator.SetTrigger(RollingHash);
        animator.SetBool(GroundedHash, true);
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        player.LastState = this;
    }
    public override void LogicUpdate()
    {
        if (rollEnd)
        {
            if (Mathf.Abs(movement.Rb.linearVelocity.y) < 0.001f)
                stateMachine.ChangeState(player.IdleState);
            else if (movement.Rb.linearVelocity.y < 0f)
            {
                animator.SetBool(GroundedHash, false);
                stateMachine.ChangeState(player.FlyingState);
            }
        }
    }
    public override void PhysicsUpdate()
    {
        if (!rollEnd)
        {
            float duration = settings.rolling.rollDuration > 0f ? settings.rolling.rollDuration : 0.0001f;
            float rollSpeed = settings.rolling.rollDistance / duration;
            movement.Rb.linearVelocity = new Vector2(rollDir * rollSpeed, movement.Rb.linearVelocity.y);

            rollElapsed += Time.fixedDeltaTime;
            if (rollElapsed >= duration)
            {
                rollEnd = true;
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
                movement.Rb.linearVelocity = new Vector2(0f, movement.Rb.linearVelocity.y);
            }
        }
    }
    public override void Exit()
    {
        base.Exit();
        animator.ResetTrigger(RollingHash);
        rollEnd = false;
    }
}