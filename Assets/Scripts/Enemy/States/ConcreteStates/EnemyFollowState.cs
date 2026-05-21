using UnityEngine;

public class FollowState : EnemyStates
{
    private static readonly int WalkingHash = Animator.StringToHash("Walking");
    private Transform playerT;

    public FollowState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        playerT = GameObject.FindWithTag("Player").transform;
        enemy.Animator.SetBool(WalkingHash, true);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (!enemy.PlayerInFollowRange)
        {
            enemy.Animator.SetBool(WalkingHash, false);
            stateMachine.ChangeState(enemy.idleState);
            return;
        }
        if (enemy.PlayerInHitRange)
        {
            enemy.Animator.SetBool(WalkingHash, false);
            stateMachine.ChangeState(enemy.hitState);
            return;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        Vector2 currentPos = enemy.transform.position;
        float targetX = playerT.position.x;
        Vector2 targetPos = new(targetX, currentPos.y);
        float step = enemy.EnemySpeed * Time.fixedDeltaTime;
        Vector2 newPos = Vector2.MoveTowards(currentPos, targetPos, step);
        enemy.transform.position = newPos;

        float dirX = targetPos.x - currentPos.x;
        if (Mathf.Abs(dirX) > 0.01f)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.flipX = dirX > 0f;
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
        enemy.Animator.SetBool(WalkingHash, false);
    }
}
