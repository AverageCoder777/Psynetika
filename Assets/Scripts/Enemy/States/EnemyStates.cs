using UnityEngine;

public abstract class EnemyStates
{
    private static readonly int WalkingHash = Animator.StringToHash("Walking");
    private static readonly int IdleHash = Animator.StringToHash("Idle");

    protected readonly EnemyController controller;
    protected readonly EnemyStateMachine stateMachine;

    protected EnemyConfig Config => controller.Config;
    protected EnemyMovement Movement => controller.Movement;
    protected EnemyAttack Attack => controller.Attack;
    protected EnemySensor Sensor => controller.Sensor;
    protected Animator Animator => controller.Animator;

    // Куда уходить, потеряв игрока: домой, если так настроено, иначе в покой на месте.
    protected EnemyStateId LostTargetStateId =>
        Config != null && Config.home != null && Config.home.returnWhenLost && controller.HasState(EnemyStateId.Return)
            ? EnemyStateId.Return
            : EnemyStateId.Rest;

    protected EnemyStates(EnemyController controller, EnemyStateMachine stateMachine)
    {
        this.controller = controller;
        this.stateMachine = stateMachine;
    }

    protected bool ChangeState(EnemyStateId id) => controller.ChangeState(id);
    protected void SetFlag(int parameterHash, bool value) => controller.SetAnimatorBool(parameterHash, value);
    protected void PlayTrigger(int parameterHash) => controller.SetAnimatorTrigger(parameterHash);

    /*
    Общая реакция на игрока, одинаковая для покоя, возвращения и боя.
    Возвращает true, если переход произошёл — вызывающему состоянию нужно сразу выйти из метода.
    */
    protected bool TryReactToPlayer()
    {
        if (Sensor.PlayerInHitRange && Attack.HasReadyAttack)
        {
            return ChangeState(EnemyStateId.Attack);
        }
        if (Sensor.PlayerInFollowRange)
        {
            return ChangeState(EnemyStateId.Engage);
        }
        return false;
    }

    // Флаги ходьбы/покоя по факту движения, а не по состоянию: упёршийся в обрыв враг стоит.
    protected void UpdateLocomotionFlags()
    {
        bool moving = Movement.IsMoving;
        SetFlag(WalkingHash, moving);
        SetFlag(IdleHash, !moving);
    }

    protected void ClearLocomotionFlags()
    {
        SetFlag(WalkingHash, false);
        SetFlag(IdleHash, false);
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void HandleInput() { }
    public virtual void LogicUpdate() { }
    public virtual void PhysicsUpdate() { }
}
