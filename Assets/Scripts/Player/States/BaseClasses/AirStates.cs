using UnityEngine;

public abstract class AirStates : State
{
    public AirStates(Player player, StateMovMachine stateMachine)
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

        if (player.Rb.linearVelocity.y <= 0 && DetectPlatform())
        {
            stateMachine.ChangeState(player.IdleState);
            return;
        }

        bool touchingWall = DetectWall();
        if (touchingWall && (player.GetCurrentCharState() != player.SatanState))
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
        Debug.Log("Player velocity y: " + player.Rb.linearVelocity.y);
    }
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        float targetVelocityX = player.MovementInput.x * player.GetCharSpeed() * 0.75f;//0.75 - фактор скорости перемещения в воздухе, добавить переменную!!!
        float currentVelocityX = player.Rb.linearVelocity.x;

        float newVelocityX = currentVelocityX;
        if (player.MovementInput.x != 0)
        {
            newVelocityX = Mathf.Lerp(currentVelocityX, targetVelocityX, player.AccelerationRate * Time.fixedDeltaTime);
        }
        else if (Mathf.Abs(currentVelocityX) > 0.1f)
        {
            newVelocityX = currentVelocityX;
        }
        player.Rb.linearVelocity = new Vector2(newVelocityX, player.Rb.linearVelocity.y);

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
        Vector2 wallDetectionDirection = player.ActiveSR.flipX ? Vector2.left : Vector2.right;
        Vector2 raycastOrigin = (Vector2)player.transform.position + wallDetectionDirection * 0.25f;

        RaycastHit2D hit = Physics2D.Raycast(
            raycastOrigin,
            wallDetectionDirection,
            player.WallDetectionDistance,
            LayerMask.GetMask("Walls")
        );
        Debug.DrawRay(raycastOrigin, wallDetectionDirection * player.WallDetectionDistance,
            hit.collider != null ? Color.green : Color.red);

        return hit.collider != null;
    }
    protected bool DetectFloor()
    {
        Vector2 floorDetectionDirection = Vector2.down;
        Vector2 raycastOrigin = (Vector2)player.transform.position - Vector2.up * 0.5f;
        float detectionDistance = 0.6f;

        RaycastHit2D hit = Physics2D.Raycast(
            raycastOrigin,
            floorDetectionDirection,
            detectionDistance,
            LayerMask.GetMask("Floor")
        );
        Debug.DrawRay(raycastOrigin, floorDetectionDirection * detectionDistance,
            hit.collider != null ? Color.blue : Color.yellow);

        return hit.collider != null;
    }
    protected bool DetectPlatform()
    {
        Vector2 platformDetectionDirection = Vector2.down;
        Vector2 raycastOrigin = (Vector2)player.transform.position - Vector2.up * 0.5f;
        float detectionDistance = 1f;

        RaycastHit2D hit = Physics2D.Raycast(
            raycastOrigin,
            platformDetectionDirection,
            detectionDistance,
            LayerMask.GetMask("Platform")
        );

        Debug.DrawRay(raycastOrigin, platformDetectionDirection * detectionDistance,
            hit.collider != null ? Color.blue : Color.yellow);

        return hit.collider != null;
    }
}
