using UnityEngine;

public abstract class AirStates : State
{
    public AirStates(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
        : base(player, stateMachine, settings) { }
    protected float wallContactTime = 0f;
    public override void Enter()
    {
        base.Enter();
    }
    public override void HandleInput()
    {
        base.HandleInput();
        movement.MovementInput = movement.PlayerInput.actions["Move"].ReadValue<Vector2>();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (movement.Rb.linearVelocity.y <= 0 && DetectFloor()=="Floor")
        {
            stateMachine.ChangeState(player.IdleState);
            return;
        }

        bool touchingWall = DetectWall();
        if (touchingWall && (charManager.GetCurrentCharacterType() != PlayerCharacterType.Satan))
        {
            wallContactTime += Time.deltaTime;
            if (wallContactTime >= settings.wall.wallWaitTime)
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
        float targetVelocityX = movement.MovementInput.x * movement.GetCurrentSpeed() * 0.75f;//0.75 - фактор скорости перемещения в воздухе, добавить переменную!!!
        float currentVelocityX = movement.Rb.linearVelocity.x;

        float newVelocityX = currentVelocityX;
        if (movement.MovementInput.x != 0)
        {
            newVelocityX = Mathf.Lerp(currentVelocityX, targetVelocityX, settings.move.accelerationRate * Time.fixedDeltaTime);
        }
        else if (Mathf.Abs(currentVelocityX) > 0.1f)
        {
            newVelocityX = currentVelocityX;
        }
        movement.Rb.linearVelocity = new Vector2(newVelocityX, movement.Rb.linearVelocity.y);

        if (movement.MovementInput.x > 0.01f)
            charManager.ActiveSR.flipX = false;
        else if (movement.MovementInput.x < -0.01f)
            charManager.ActiveSR.flipX = true;
        if (movement.Rb.linearVelocity.y > 0)
        {
            movement.Rb.gravityScale = settings.jump.upGravityScale;
        }
        else
        {
            movement.Rb.gravityScale = settings.jump.downGravityScale;
        }
    }
    public override void Exit()
    {
        base.Exit();
    }
    protected bool DetectWall()
    {
        Vector2 wallDetectionDirection = charManager.ActiveSR.flipX ? Vector2.left : Vector2.right;
        Vector2 raycastOrigin = (Vector2)player.transform.position + wallDetectionDirection * 0.25f;

        RaycastHit2D hit = Physics2D.Raycast(
            raycastOrigin,
            wallDetectionDirection,
            settings.wall.wallDetectionDistance,
            LayerMask.GetMask("Walls")
        );
        #if UNITY_EDITOR
        if (player.debugMessages)
        {
            Debug.DrawRay(raycastOrigin, wallDetectionDirection * settings.wall.wallDetectionDistance,
                hit.collider != null ? Color.green : Color.red);
        }
        #endif
        return hit.collider != null;
    }
    protected string DetectFloor()
    {
        Vector2 floorDetectionDirection = Vector2.down;
        Vector2 platformDetectionDirection = Vector2.down;
        Vector2 raycastOrigin = (Vector2)player.transform.position - Vector2.up * 0.5f;
        float detectionDistance = 0.8f;

        RaycastHit2D hitFloor = Physics2D.Raycast(
            raycastOrigin,
            floorDetectionDirection,
            detectionDistance,
            LayerMask.GetMask("Floor")
        );
        RaycastHit2D hitPlatform = Physics2D.Raycast(
            raycastOrigin,
            platformDetectionDirection,
            detectionDistance,
            LayerMask.GetMask("Platform")
        );
        #if UNITY_EDITOR
        if (player.debugMessages)
        {
            Debug.DrawRay(raycastOrigin, floorDetectionDirection * detectionDistance,
                hitFloor.collider != null ? Color.blue : Color.yellow);
            Debug.DrawRay(raycastOrigin, platformDetectionDirection * detectionDistance,
                hitPlatform.collider != null ? Color.blue : Color.yellow);
        }
        #endif
        if (hitFloor.collider != null)
        {
            return "Floor";
        }
        if (hitPlatform.collider != null)
        {
            return "Platform";
        }
        return "None";
    }
}
