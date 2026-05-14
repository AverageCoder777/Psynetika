using UnityEngine;

public class EnemyHitState : EnemyStates
{
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private float hitElapsed = 0f;
    private float HitDuration => enemy.EnemyHitDuration;
    private float hitSpeed => enemy.EnemySpeed;
    private int HitDamage => enemy.EnemyDamage;
    private bool hitCompleted = false;
    private bool damageDone = false;
    private Player playerScript;

    public EnemyHitState(Enemy enemy, EnemyStateMachine stateMachine)
        : base(enemy, stateMachine) { }

    public override void Enter()
    {
        if (playerScript == null)
        {
            playerScript = GameObject.FindWithTag("Player").GetComponent<Player>();
        }
        enemy.Animator.SetTrigger(HitHash);
        hitElapsed = 0f;
        hitCompleted = false;
        damageDone = false;
    }

    public override void LogicUpdate()
    {
        if (enemy.PlayerInHitRange)
        {
            if (!hitCompleted)
            {
                enemy.Animator.SetBool(HitHash, true);
                hitElapsed += Time.deltaTime;
                if (hitElapsed >= HitDuration && !damageDone)
                {
                    playerScript.TakeDamage(HitDamage);
                    damageDone = true;
                }
                if (hitElapsed >= HitDuration)
                {
                    enemy.Animator.SetBool(HitHash, false);
                    hitCompleted = true;
                }
            }
            if (hitCompleted)
            {
                stateMachine.ChangeState(enemy.followState);
            }
        }
        else
        {
            enemy.Animator.SetBool(HitHash, false);
            stateMachine.ChangeState(enemy.idleState);
        }
    }

    public override void PhysicsUpdate()
    {
    }

    public override void Exit()
    {
        enemy.Animator.SetBool(HitHash, false);
        hitElapsed = 0f;
        hitCompleted = false;
        damageDone = false;
    }
}
