using UnityEngine;

public class WallState : State
{
    private bool jumpInput = false;
    private float wallJumpThrustFactor = 1f;
    private Vector2 wallSurfaceNormal = Vector2.zero; // Нормаль поверхности стены

    public WallState(Player player, StateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        animator.SetBool("WallSliding", true);
        player.Rb.gravityScale = 1f;
        if (player.DebugMessages)
        {
            Debug.Log("Entered Wall State");
        }
    }

    public override void HandleInput()
    {
        base.HandleInput();
        jumpInput = player.PlayerInput.actions["Jump"].WasPressedThisFrame();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // Проверяем контакт со стеной и сохраняем нормаль
        DetectWall();

        // Если достигли земли
        if (player.Rb.linearVelocity.y == 0)
        {
            stateMachine.ChangeState(player.IdleState);
            return;
        }

        // Если нажата кнопка прыжка, прыгаем от стены
        if (jumpInput)
        {
            WallJump();
            stateMachine.ChangeState(player.AirState);
        }
    }

    public override void PhysicsUpdate()
    {
        // Слайд по стене с уменьшенной скоростью падения
        float slideVelocity = -player.WallSlideSpeed;
        player.Rb.linearVelocity = new Vector2(0, slideVelocity);
    }

    private void WallJump()
    {
        // Прыгаем используя нормаль стены для направления отталкивания
        // Горизонтальная скорость - отталкивание от стены по её нормали
        float horizontalVelocity = wallSurfaceNormal.x * player.Speed*wallJumpThrustFactor;
        // Вертикальная скорость - прыжок вверх
        float verticalVelocity = Mathf.Sqrt(player.WallJumpForce * 2f);
        
        player.Rb.linearVelocity = new Vector2(horizontalVelocity, verticalVelocity);
        
        // Разворачиваем персонажа в сторону отталкивания, чтобы избежать повторного прилипания к стене
        player.ActiveSR.flipX = player.ActiveSR.flipX ? false : true;

        if (player.DebugMessages)
        {
            Debug.Log("Wall Jump executed. Normal: " + wallSurfaceNormal + ", horizontalVelocity: " + horizontalVelocity + ", total velocity: " + player.Rb.linearVelocity);
        }
    }

    public override void Exit()
    {
        animator.SetBool("WallSliding", false);
        if (player.DebugMessages)
        {
            Debug.Log("Exited Wall State");
        }
        base.Exit();
    }

    private void DetectWall()
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

        // Отрисовка raycast для отладки (зелёный - столкновение, красный - нет)
        Debug.DrawRay(raycastOrigin, wallDetectionDirection * player.WallDetectionDistance,
            hit.collider != null ? Color.green : Color.red);
        
        if (hit.collider != null)
        {
            // Сохраняем нормаль поверхности стены
            wallSurfaceNormal = hit.normal;
        }
    }
}
