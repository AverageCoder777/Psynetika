using UnityEngine;

public abstract class EnemyStates
{
    protected readonly EnemyController controller;
    protected readonly EnemyStateMachine stateMachine;

    protected EnemyConfig Config => controller.Config;
    protected EnemyMovement Movement => controller.Movement;
    protected EnemyAttack Attack => controller.Attack;
    protected EnemySensor Sensor => controller.Sensor;
    protected Animator Animator => controller.Animator;

    // Куда возвращаться, когда игрок потерян: на патруль, если он включён и зарегистрирован.
    protected EnemyStateId RestStateId =>
        Config != null && Config.patrol != null && Config.patrol.enabled && controller.HasState(EnemyStateId.Patrol)
            ? EnemyStateId.Patrol
            : EnemyStateId.Idle;

    protected EnemyStates(EnemyController controller, EnemyStateMachine stateMachine)
    {
        this.controller = controller;
        this.stateMachine = stateMachine;
    }

    protected bool ChangeState(EnemyStateId id) => controller.ChangeState(id);
    protected void SetFlag(int parameterHash, bool value) => controller.SetAnimatorBool(parameterHash, value);
    protected void PlayTrigger(int parameterHash) => controller.SetAnimatorTrigger(parameterHash);

    /*
    Общая реакция на игрока, одинаковая для покоя, патруля и преследования.
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
            return ChangeState(EnemyStateId.Follow);
        }
        return false;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void HandleInput() { }
    public virtual void LogicUpdate() { }
    public virtual void PhysicsUpdate() { }
}
