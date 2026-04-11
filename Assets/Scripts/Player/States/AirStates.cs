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
        player.MovementInput = player.PlayerInput.actions["Move"].ReadValue<Vector2>();
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
    }
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        float targetVelocityX = player.MovementInput.x * player.Speed* 0.75f;//0.5 - фактор скорости перемещения в воздухе, добавить переменную!!!
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
        // Определяем направление взгляда игрока
        Vector2 wallDetectionDirection = player.ActiveSR.flipX ? Vector2.left : Vector2.right;
        
        // Смещаем начальную позицию raycast чтобы избежать собственного коллайдера игрока
        Vector2 raycastOrigin = (Vector2)player.transform.position + wallDetectionDirection * 0.1f;
        
        // Проверяем столкновение только в направлении взгляда
        RaycastHit2D hit = Physics2D.Raycast(
            raycastOrigin,
            wallDetectionDirection,
            player.WallDetectionDistance,
            LayerMask.GetMask("Walls")
        );

        // Отрисовка raycast для отладки (зеленый - столкновение, красный - нет)
        Debug.DrawRay(raycastOrigin, wallDetectionDirection * player.WallDetectionDistance, 
            hit.collider != null ? Color.green : Color.red);

        return hit.collider != null;
    }
}
