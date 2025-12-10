using UnityEngine;

public class EnemyIdleState : EnemyStates
{
    public EnemyIdleState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }
    public override void Enter()
    {
        enemy.Animator.SetBool("Idle", true);
        Debug.Log("Enemy Entered Idle State");
    }
    public override void LogicUpdate()
    {
        if (enemy.PlayerInFollowRange)
        {
            enemy.Animator.SetBool("Idle", false);
            stateMachine.ChangeState(enemy.followState);
            if (enemy.PlayerInHitRange)
            {
                enemy.Animator.SetBool("Idle", false);
                stateMachine.ChangeState(enemy.hitState);
            }
        }
    }
    public override void Exit()
    {
        enemy.Animator.SetBool("Idle", false);
        Debug.Log("Enemy Exited Idle State");
    }
}
