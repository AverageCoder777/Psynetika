using UnityEngine;

// Зеркалит EnemyAttackState, но в конце цикла вместо ближнего удара делает выстрел.
public class EnemyRangedAttackState : EnemyStates
{
    private static readonly int ShootHash = Animator.StringToHash("Shoot");

    private float elapsed;
    private bool cycleCompleted;
    private bool shotDone;

    public EnemyRangedAttackState(EnemyController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    public override void Enter()
    {
        // Повернуться к игроку, чтобы пуля летела в него, а не в последнюю сторону ходьбы.
        Transform target = Sensor.PlayerTransform;
        if (target != null)
        {
            Movement.Face(target.position.x - controller.transform.position.x);
        }

        Animator.SetTrigger(ShootHash);
        elapsed = 0f;
        cycleCompleted = false;
        shotDone = false;
    }

    public override void LogicUpdate()
    {
        if (!Sensor.PlayerInHitRange)
        {
            stateMachine.ChangeState(controller.IdleState);
            return;
        }

        if (!cycleCompleted)
        {
            Animator.SetBool(ShootHash, true);
            elapsed += Time.deltaTime;
            if (elapsed >= Attack.AttackDuration)
            {
                if (!shotDone)
                {
                    Attack.SpawnBullet();
                    shotDone = true;
                }
                Animator.SetBool(ShootHash, false);
                cycleCompleted = true;
            }
        }
        else
        {
            stateMachine.ChangeState(controller.FollowState);
        }
    }

    public override void Exit()
    {
        Animator.SetBool(ShootHash, false);
        elapsed = 0f;
        cycleCompleted = false;
        shotDone = false;
    }
}
