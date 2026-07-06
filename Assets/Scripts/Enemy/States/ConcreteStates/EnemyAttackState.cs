using UnityEngine;

public class EnemyAttackState : EnemyStates
{
    private static readonly int HitHash = Animator.StringToHash("Hit");

    private float hitElapsed;
    private bool hitCompleted;
    private bool damageDone;

    public EnemyAttackState(EnemyController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    public override void Enter()
    {
        Animator.SetTrigger(HitHash);
        hitElapsed = 0f;
        hitCompleted = false;
        damageDone = false;
    }

    public override void LogicUpdate()
    {
        if (!Sensor.PlayerInHitRange)
        {
            stateMachine.ChangeState(controller.IdleState);
            return;
        }

        if (!hitCompleted)
        {
            Animator.SetBool(HitHash, true);
            hitElapsed += Time.deltaTime;
            // Урон в конце замаха, если игрок всё ещё в зоне удара.
            if (hitElapsed >= Attack.AttackDuration)
            {
                if (!damageDone)
                {
                    Attack.DealMeleeDamage(Sensor.PlayerTarget);
                    damageDone = true;
                }
                Animator.SetBool(HitHash, false);
                hitCompleted = true;
            }
        }
        else
        {
            stateMachine.ChangeState(controller.FollowState);
        }
    }

    public override void Exit()
    {
        Animator.SetBool(HitHash, false);
        hitElapsed = 0f;
        hitCompleted = false;
        damageDone = false;
    }
}
