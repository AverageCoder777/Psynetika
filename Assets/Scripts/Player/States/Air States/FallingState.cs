using UnityEngine;

public class FallingState : AirStates
{
    private float wallContactTime = 0f;

    public FallingState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        animator.SetTrigger("Falling");
        player.Rb.gravityScale = player.DownGravityScale;
        wallContactTime = 0f;
        if (player.DebugMessages) Debug.Log("Entered Falling State");
    }

    public override void HandleInput()
    {
        base.HandleInput();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        player.Rb.gravityScale = player.DownGravityScale;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();//Подтягиваем логику переменной moving
        
        // Проверяем столкновение со стеной
        if (player.Rb.linearVelocity.y < 0) // Падаем вниз
        {
            bool touchingWall = DetectWall();
            
            if (touchingWall)
            {
                wallContactTime += Time.deltaTime;
                if (wallContactTime >= player.WallWaitTime)
                {
                    stateMachine.ChangeState(player.WallState);
                    return;
                }
            }
            else
            {
                wallContactTime = 0f;
            }
        }
        
        if (player.Rb.linearVelocity.y == 0) 
        {
            stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void Exit()
    {
        player.Rb.gravityScale = 1f;
        animator.ResetTrigger("Falling");
        if (player.DebugMessages) Debug.Log("Exited Falling State");
    }
}