public abstract class PlayerState
{
    protected Player player;
    protected StateCharMachine stateMachine;
    public PlayerState(Player player, StateCharMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;
    }
    public virtual void Enter(){}
    public virtual void LogicUpdate(){}
    public virtual void PhysicsUpdate(){}
    public virtual void Exit(){}

    
}
