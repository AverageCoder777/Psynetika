using UnityEngine;

public abstract class AirStates : State
{
    public AirStates(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {
    }
    protected float wallContactTime = 0f;
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

        //Базовый детект стены для всех состояний в воздухе
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
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        float targetVelocityX = player.MovementInput.x * player.Speed* 0.75f;//0.75 - фактор скорости перемещения в воздухе, добавить переменную!!!
        float currentVelocityX = player.Rb.linearVelocity.x;
        
        // Выбираем коэффициент ускорения/трения
        float newVelocityX = currentVelocityX;
        if (player.MovementInput.x != 0)
        {
            // Есть входи в движение - используем ускорение
            newVelocityX = Mathf.Lerp(currentVelocityX, targetVelocityX, player.AccelerationRate * Time.fixedDeltaTime);
        }
        else if (Mathf.Abs(currentVelocityX) > 0.1f)
        {
            // Нет ввода от игрока, но есть инерция (от wall jump или прыжка) - медленнее снижаем скорость
            newVelocityX = currentVelocityX;
        }
        
        // Плавно интерполируем текущую скорость к целевой для эффекта инерции
        
        // Применяем новое ускорение, но оставляем горизонтальное неизменным
        player.Rb.linearVelocity = new Vector2(newVelocityX, player.Rb.linearVelocity.y);
        
        // Крутим спрайт в зависимости от направления
        if (player.MovementInput.x > 0.01f)
            player.ActiveSR.flipX = false;
        else if (player.MovementInput.x < -0.01f)
            player.ActiveSR.flipX = true;
        if (player.Rb.linearVelocity.y > 0)
        {
            player.Rb.gravityScale = player.UpGravityScale;
        }
        else
        {
            player.Rb.gravityScale = player.DownGravityScale;
        }
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
        Vector2 raycastOrigin = (Vector2)player.transform.position + wallDetectionDirection * 0.25f;
        
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
