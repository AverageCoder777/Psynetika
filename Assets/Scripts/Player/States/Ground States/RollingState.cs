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
        rollEnd = false;
        rollDir = charManager.ActiveSR != null && charManager.ActiveSR.flipX ? -1f : 1f;
        charManager.ActiveAnimator.SetTrigger(RollingHash);
        charManager.ActiveAnimator.SetBool(GroundedHash, true);
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
    }
    public override void LogicUpdate()
    {
        // Прыжок из переката: разгон переката сохраняется — получается длинный «прыжок с разбега».
        if (settings.rolling.jumpCancel && movement.JumpPressedThisFrame)
        {
            stateMachine.ChangeState(player.JumpingState);
            return;
        }

        if (!rollEnd)
        {
            movement.MarkGrounded();
            return;
        }

        if (Mathf.Abs(movement.Rb.linearVelocity.y) < settings.detection.movementInputThreshold)
            stateMachine.ChangeState(player.IdleState);
        else if (movement.Rb.linearVelocity.y < 0f)
        {
            charManager.ActiveAnimator.SetBool(GroundedHash, false);
            stateMachine.ChangeState(player.FlyingState);
        }
    }
    public override void PhysicsUpdate()
    {
        if (!rollEnd)
        {
            float duration = settings.rolling.rollDuration;
            float rollSpeed = settings.rolling.rollDistance / duration;
            movement.Rb.linearVelocity = new Vector2(rollDir * rollSpeed, movement.Rb.linearVelocity.y);

            rollElapsed += Time.fixedDeltaTime;
            if (rollElapsed >= duration)
            {
                rollEnd = true;
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);

                // Зажато направление переката — выходим сразу на скорость бега, без замирания.
                bool keepRunning = settings.rolling.keepRunSpeedOnExit && movementInput.x * rollDir > 0.01f;
                float exitSpeedX = keepRunning ? rollDir * movement.GetCurrentSpeed() : 0f;
                movement.Rb.linearVelocity = new Vector2(exitSpeedX, movement.Rb.linearVelocity.y);
            }
        }
    }
    public override void Exit()
    {
        base.Exit();
        charManager.ActiveAnimator.ResetTrigger(RollingHash);
        // Перекат мог прерваться (прыжок, смерть, захват) до конца — неуязвимость к врагам снимаем всегда.
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        rollEnd = false;
    }
}
