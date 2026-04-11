using UnityEngine;

public abstract class AirStates : State
{
    public AirStates(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {
    }
    public override void Enter()
    {
        base.Enter();
    }
    public override void HandleInput()
    {
        base.HandleInput();
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
    }
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
    public override void Exit()
    {
        base.Exit();
    }
    protected bool DetectWall()
    {
        // Проверяем столкновение справа
        RaycastHit2D hitRight = Physics2D.Raycast(
            player.transform.position,
            Vector2.right,
            player.WallDetectionDistance,
            LayerMask.GetMask("Walls")
        );

        // Проверяем столкновение слева
        RaycastHit2D hitLeft = Physics2D.Raycast(
            player.transform.position,
            Vector2.left,
            player.WallDetectionDistance,
            LayerMask.GetMask("Walls")
        );

        return hitRight.collider != null || hitLeft.collider != null;
    }
}
