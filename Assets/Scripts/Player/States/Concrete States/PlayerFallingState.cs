using UnityEngine;

public class PlayerFallingState : PlayerState
{
    public PlayerFallingState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }
    public override void Enter()
    {
        animator.SetTrigger("Falling");
        Debug.Log("Entered Falling State");
    }
    public override void HandleInput()
    {
        base.HandleInput();
    }
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
    public override void LogicUpdate()
    {
        if (player.Rb.linearVelocity.y == 0) stateMachine.ChangeState(player.IdleState);
    }
    public override void Exit()
    {
        animator.ResetTrigger("Falling");
        Debug.Log("Exited Falling State");
    }
}