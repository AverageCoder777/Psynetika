using UnityEngine;

public class WallState : State
{
    private bool wallSliding = false;
    private int wallDirection = 1; // 1 = справа, -1 = слева
    private bool jumpInput = false;

    public WallState(Player player, StateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        wallSliding = true;
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
        
        // Проверяем, есть ли ещё контакт со стеной
        bool touchingWall = DetectWall(out int detectedWallDirection);
        wallDirection = detectedWallDirection;
        
        if (!touchingWall)
        {
            // Нет контакта со стеной - переходим в FallingState
            if (player.Rb.linearVelocity.y <= 0)
            {
                stateMachine.ChangeState(player.FallingState);
            }
            return;
        }

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
            stateMachine.ChangeState(player.JumpingState);
        }
    }

    public override void PhysicsUpdate()
    {
        // Слайд по стене с уменьшенной скоростью падения
        float slideVelocity = -player.WallSlideSpeed;
        player.Rb.linearVelocity = new Vector2(0, slideVelocity);

        // Тормозим горизонтальное движение
        player.ActiveSR.flipX = (wallDirection == -1);
    }

    private void WallJump()
    {
        // Прыгаем от стены в направлении, противоположном стене
        Vector2 wallJumpDirection = new Vector2(-wallDirection, 1).normalized;
        player.Rb.linearVelocity = Vector2.zero;
        player.Rb.AddForce(wallJumpDirection * player.WallJumpForce, ForceMode2D.Impulse);
        
        if (player.DebugMessages)
        {
            Debug.Log("Wall Jump executed. Direction: " + wallJumpDirection);
        }
    }

    private bool DetectWall(out int detectedDirection)
    {
        detectedDirection = 1;

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

        if (hitRight.collider != null)
        {
            detectedDirection = 1;
            return true;
        }
        
        if (hitLeft.collider != null)
        {
            detectedDirection = -1;
            return true;
        }

        return false;
    }

    public override void Exit()
    {
        animator.SetBool("WallSliding", false);
        wallSliding = false;
        if (player.DebugMessages)
        {
            Debug.Log("Exited Wall State");
        }
        base.Exit();
    }
}
