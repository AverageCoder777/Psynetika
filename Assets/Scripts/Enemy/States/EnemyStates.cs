using UnityEngine;

public abstract class EnemyStates
{
    protected readonly EnemyController controller;
    protected readonly EnemyStateMachine stateMachine;

    protected EnemyMovement Movement => controller.Movement;
    protected EnemyAttack Attack => controller.Attack;
    protected EnemySensor Sensor => controller.Sensor;
    protected Animator Animator => controller.Animator;

    protected EnemyStates(EnemyController controller, EnemyStateMachine stateMachine)
    {
        this.controller = controller;
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void HandleInput() { }
    public virtual void LogicUpdate() { }
    public virtual void PhysicsUpdate() { }
}
