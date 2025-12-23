using UnityEngine;

public class EnemyHitState : EnemyStates
{
    private float hitElapsed = 0f;
    private float hitDuration => enemy.EnemyHitDuration;
    private float hitSpeed => enemy.EnemySpeed;
    private int hitDamage => enemy.EnemyDamage;
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
        enemy.Animator.SetTrigger("Hit");
        Debug.Log("Enemy Entered Hit State");
        hitElapsed = 0f;
        hitCompleted = false;
        damageDone = false;
    }

    public override void LogicUpdate()
    {
        if (!hitCompleted&&enemy.PlayerInHitRange)
        {
            enemy.Animator.SetBool("Hit", true);
            hitElapsed += Time.deltaTime;
            if (hitElapsed >= hitDuration && !damageDone)
            {
                playerScript.TakeDamage(hitDamage);
                damageDone = true;
            }
            if (hitElapsed >= hitDuration)
            {
                enemy.Animator.SetBool("Hit", false);
                hitCompleted = true;
                stateMachine.ChangeState(enemy.idleState);
            }
        }
        else if (!enemy.PlayerInHitRange)
        {
            enemy.Animator.SetBool("Hit", false);
            stateMachine.ChangeState(enemy.idleState);
        }
    }

    public override void PhysicsUpdate()
    {
    }

    public override void Exit()
    {
        enemy.Animator.SetBool("Hit", false);
        Debug.Log("Enemy Exited Hit State");
        hitElapsed = 0f;
        hitCompleted = false;
        damageDone = false;
    }
}
