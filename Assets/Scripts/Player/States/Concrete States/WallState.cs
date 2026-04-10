using UnityEngine;

public class WallState : States
{
    public WallState(Player player, StateMachine stateMachine)
        : base (player, stateMachine){}
    public override void Enter(){
        Debug.Log("Entered Wall State");
    }
    public override void HandleInput(){
        base.HandleInput();
    }
    public override void PhysicsUpdate(){
        base.PhysicsUpdate();
    }
    public override void LogicUpdate(){
        base.LogicUpdate();
        if (player.Rb.linearVelocity.y == 0)
        {
            stateMachine.ChangeState(player.IdleState);
        }
    }
    public override void Exit(){
        Debug.Log("Exited Wall State");
        base.Exit();
    }
}
