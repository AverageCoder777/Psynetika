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
        float targetVelocityX = player.MovementInput.x * player.Speed;
        float currentVelocityX = player.Rb.linearVelocity.x;
        
        // Выбираем знак (+/-) для ускорения
        float accelerationToUse = player.MovementInput.x != 0 ? player.AccelerationRate : player.FrictionRate;
        
        // Плавно интерполируем текущую скорость к целевой для эффекта инерции
        float newVelocityX = Mathf.Lerp(currentVelocityX, targetVelocityX, accelerationToUse * Time.fixedDeltaTime);
        
        // Применяем новое ускорение, но оставляем горизонтальное неизменным
        player.Rb.linearVelocity = new Vector2(newVelocityX, player.Rb.linearVelocity.y);
        
        // Крутим спрайт в зависимости от направления
        if (player.MovementInput.x > 0.01f)
            player.ActiveSR.flipX = false;
        else if (player.MovementInput.x < -0.01f)
            player.ActiveSR.flipX = true;
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
