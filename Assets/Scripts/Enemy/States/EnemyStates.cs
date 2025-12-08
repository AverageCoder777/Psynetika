using UnityEngine;
public abstract class EnemyStates
{
    protected Enemy enemy;
    protected EnemyStateMachine stateMachine;
    protected Animator animator => enemy.Animator;
    public EnemyStates(Enemy enemy, EnemyStateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }
    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void LogicUpdate() { }

    public virtual void HandleInput(){ }
    public virtual void PhysicsUpdate(){}
}