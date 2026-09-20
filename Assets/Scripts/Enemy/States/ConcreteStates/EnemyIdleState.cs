using UnityEngine;

// Покой: враг стоит на месте, пока не заметит игрока. Если включён патруль — сразу уходит в него.
public class EnemyIdleState : EnemyStates
{
    private static readonly int IdleHash = Animator.StringToHash("Idle");

    public EnemyIdleState(EnemyController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    public override void Enter()
    {
        SetFlag(IdleHash, true);
    }

    public override void LogicUpdate()
    {
        // Один переход за кадр, приоритет атаке.
        if (TryReactToPlayer()) return;

        if (RestStateId == EnemyStateId.Patrol)
        {
            ChangeState(EnemyStateId.Patrol);
        }
    }

    public override void Exit()
    {
        SetFlag(IdleHash, false);
    }
}
