using UnityEngine;

public class EnemyIdleState : EnemyStates
{
    public EnemyIdleState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }
    private BoxCollider2D triggerColFollow;
    private BoxCollider2D triggerColAttack;
    private bool playerInHitRange = false;
    public override void Enter()
    {
        enemy.Animator.SetBool("Idle", true);
        BoxCollider2D[] colliders = enemy.GetComponents<BoxCollider2D>();
       //triggerColFollow = colliders[1];
        triggerColAttack = colliders[1];
        Debug.Log("Enemy Entered Idle State");
    }
    public override void LogicUpdate()
    {
        /*if (triggerColFollow.IsTouchingLayers(LayerMask.GetMask("Player")))
        {
            enemy.Animator.SetBool("Idle", false);
            stateMachine.ChangeState(enemy.followState);
        }*/
        if (playerInHitRange)
        {
            enemy.Animator.SetBool("Idle", false);
            stateMachine.ChangeState(enemy.hitState);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInHitRange = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInHitRange = false;
        }
    }
    public override void Exit()
    {
        enemy.Animator.SetBool("Idle", false);
        triggerColFollow = null;
        triggerColAttack = null;
        Debug.Log("Enemy Exited Idle State");
    }
}
