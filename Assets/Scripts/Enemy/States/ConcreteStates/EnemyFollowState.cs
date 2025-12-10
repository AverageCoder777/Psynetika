using UnityEngine;

public class FollowState : EnemyStates
{
    private Transform playerT;
    private Vector3 initialScale;

    public FollowState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        playerT = GameObject.FindWithTag("Player").transform;
        initialScale = enemy.transform.localScale;
        enemy.Animator.SetBool("Walking", true);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (!enemy.PlayerInFollowRange)
        {
            enemy.Animator.SetBool("Walking", false);
            stateMachine.ChangeState(enemy.idleState);
            return;
        }
        if (enemy.PlayerInHitRange)
        {
            enemy.Animator.SetBool("Walking", false);
            stateMachine.ChangeState(enemy.hitState);
            return;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        Vector2 currentPos = enemy.transform.position;
        Vector2 targetPos = playerT.position;
        float step = enemy.EnemySpeed * Time.fixedDeltaTime;
        Vector2 newPos = Vector2.MoveTowards(currentPos, targetPos, step);
        enemy.transform.position = newPos;

        float dirX = targetPos.x - currentPos.x;
        if (Mathf.Abs(dirX) > 0.01f)
        {
            Vector3 scale = initialScale;
            scale.x = (dirX > 0f) ? Mathf.Abs(initialScale.x) : -Mathf.Abs(initialScale.x);
            enemy.transform.localScale = scale;
        }
    }

    public override void Exit()
    {
        base.Exit();
        enemy.Animator.SetBool("Walking", false);
    }
}
