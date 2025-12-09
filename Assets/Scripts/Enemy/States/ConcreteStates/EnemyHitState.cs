using UnityEngine;

public class EnemyHitState : EnemyStates
{
    private float hitElapsed = 0f;
    private float hitDuration => enemy.EnemyHitDuration;
    private float hitSpeed => enemy.EnemySpeed;
    private int hitDamage => enemy.EnemyDamage;
    private bool hitCompleted = false;
    private Player player = GameObject.FindWithTag("Player").GetComponent<Player>();

    public EnemyHitState(Enemy enemy, EnemyStateMachine stateMachine)
        : base(enemy, stateMachine) { }

    public override void Enter()
    {
        enemy.Animator.SetTrigger("Hit");
        Debug.Log("Enemy Entered Hit State");
    }

    public override void LogicUpdate()
    {
        if (hitElapsed < hitDuration)
        {
            enemy.Animator.SetBool("Hit", true);
            hitElapsed += Time.deltaTime;
        }
        else
        {
            enemy.Animator.SetBool("Hit", false);
            hitCompleted = true;
            stateMachine.ChangeState(enemy.idleState);
        }
        if (hitCompleted)
        {
            player.TakeDamage(hitDamage);
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
    }
}
