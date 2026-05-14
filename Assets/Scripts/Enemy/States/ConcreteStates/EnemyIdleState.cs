using UnityEngine;

public class EnemyIdleState : EnemyStates
{
    private static readonly int IdleHash = Animator.StringToHash("Idle");

    public EnemyIdleState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }
    public override void Enter()
    {
        enemy.Animator.SetBool(IdleHash, true);
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
        enemy.Animator.SetBool(IdleHash, false);
    }
}
