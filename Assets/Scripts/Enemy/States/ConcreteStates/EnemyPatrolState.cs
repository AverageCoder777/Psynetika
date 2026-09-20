using UnityEngine;

// Патруль: ходьба туда-сюда вокруг точки спавна с паузами на краях маршрута.
// Разворачивается и по достижении края, и упершись в стену/обрыв (EnemyMovement.IsBlockedAhead).
public class EnemyPatrolState : EnemyStates
{
    private static readonly int WalkingHash = Animator.StringToHash("Walking");
    private static readonly int IdleHash = Animator.StringToHash("Idle");

    private float targetX;
    private float waitLeft;

    public EnemyPatrolState(EnemyController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    public override void Enter()
    {
        waitLeft = 0f;
        targetX = EdgeX(Movement.FacingDirection >= 0f ? 1f : -1f);
        SetFlag(WalkingHash, true);
    }

    public override void LogicUpdate()
    {
        if (TryReactToPlayer()) return;

        if (waitLeft > 0f)
        {
            waitLeft -= Time.deltaTime;
            if (waitLeft <= 0f)
            {
                SetFlag(IdleHash, false);
                SetFlag(WalkingHash, true);
            }
        }
    }

    public override void PhysicsUpdate()
    {
        if (waitLeft > 0f) return;

        bool moved = Movement.MoveTowardsX(targetX, Patrol.speedScale);
        bool arrived = Mathf.Abs(controller.transform.position.x - targetX) <= 0.05f;
        if (!moved || arrived)
        {
            TurnAround();
        }
    }

    public override void Exit()
    {
        SetFlag(WalkingHash, false);
        SetFlag(IdleHash, false);
        waitLeft = 0f;
    }

    private EnemyPatrolSettings Patrol => Config.patrol;

    private void TurnAround()
    {
        float newDirection = targetX > controller.transform.position.x ? -1f : 1f;
        targetX = EdgeX(newDirection);
        Movement.Face(newDirection);

        waitLeft = Patrol.waitTime;
        if (waitLeft > 0f)
        {
            SetFlag(WalkingHash, false);
            SetFlag(IdleHash, true);
        }
    }

    private float EdgeX(float direction) => Movement.SpawnPosition.x + direction * Patrol.distance;
}
