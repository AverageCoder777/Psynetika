using UnityEngine;

/*
Замешательство (роль Stagger): враг стоит, не атакует и не реагирует на игрока — его можно добить.
Отбрасывание, с которым он сюда попал, доигрывается: стоп-намерение не перебивает блокировку управления.
По окончании — обратно в бой или в покой, если игрок ушёл.
*/
public class EnemyStunnedState : EnemyStates
{
    private float duration = 1f;
    private int flagHash;
    private float elapsed;

    public EnemyStunnedState(EnemyController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    // Войти в замешательство на seconds секунд с bool аниматора animatorParameter (пусто — без него).
    public bool Begin(float seconds, string animatorParameter)
    {
        duration = Mathf.Max(0f, seconds);
        flagHash = string.IsNullOrEmpty(animatorParameter) ? 0 : Animator.StringToHash(animatorParameter);
        return ChangeState(EnemyStateId.Stagger);
    }

    public override void Enter()
    {
        elapsed = 0f;
        ClearLocomotionFlags();
        if (flagHash != 0) SetFlag(flagHash, true);
    }

    public override void LogicUpdate()
    {
        elapsed += Time.deltaTime;
        if (elapsed < duration) return;

        ChangeState(Sensor.PlayerInFollowRange || Sensor.PlayerInHitRange ? EnemyStateId.Engage : LostTargetStateId);
    }

    public override void PhysicsUpdate()
    {
        Movement.Execute(MoveIntent.Stop);
    }

    public override void Exit()
    {
        if (flagHash != 0) SetFlag(flagHash, false);
    }
}
