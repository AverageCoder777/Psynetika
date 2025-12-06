using UnityEngine;

public class PlayerCrouchingState : PlayerGroundedState
{
    BoxCollider2D capsule;
    Vector2 originalCapsuleSize;
    Vector2 originalCapsuleOffset;
    private LayerMask obstacleMask = LayerMask.GetMask("Walls"); // Слой препятствий
    private float headCheckDistanceBuffer = 0.02f;
    private bool crouchHeld;

    public PlayerCrouchingState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();
        BoxCollider2D[] colliders = player.GetComponents<BoxCollider2D>();
        capsule = colliders[1];
        originalCapsuleSize = capsule.size;
        originalCapsuleOffset = capsule.offset;
        Debug.Log("Entered Crouching State");
        Vector2 newSize = new(originalCapsuleSize.x, originalCapsuleSize.y * player.CrouchHeightMultiplier);
        float delta = originalCapsuleSize.y - newSize.y;
        capsule.size = newSize;
        capsule.offset = new Vector2(originalCapsuleOffset.x, originalCapsuleOffset.y - delta / 2f);
        animator.SetBool("Crouching", true);
    }
    public override void HandleInput()
    {
        base.HandleInput();
        crouchHeld = player.playerInput.actions["Crouch"].IsPressed();
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (!crouchHeld)
        {
            stateMachine.ChangeState(player.IdleState);
        }
    }
    public override void PhysicsUpdate()
    {
        // Base не применяем от дочернего класса, чтобы не было движения с полной скоростью
        player.Rb.linearVelocity = new Vector2(player.MovementInput.x * player.Speed * 0.5f, player.Rb.linearVelocity.y);
        if (player.MovementInput.x > 0.01f)
            player.GetActiveSpriteRenderer().flipX = false;
        else if (player.MovementInput.x < -0.01f)
            player.GetActiveSpriteRenderer().flipX = true;
    }
    public override void Exit()
    {
        base.Exit();
        StopCrouch();
        animator.SetBool("Crouching", false);
        Debug.Log("Exited Crouching State");
    }
    public bool StopCrouch()
    {
        Vector2 crouchCenter = (Vector2)player.transform.position + capsule.offset;
        float crouchTop = crouchCenter.y + (capsule.size.y / 2f) - headCheckDistanceBuffer;

        // Дистанция проверки = delta высоты + buffer
        float deltaHeight = originalCapsuleSize.y - capsule.size.y;
        float headroomNeeded = deltaHeight + headCheckDistanceBuffer;

        // 3 луча: left, center, right для покрытия ширины
        float halfWidth = capsule.size.x / 2f * 0.8f; // 80% ширины, чтобы не ловить бока
        Vector2 originCenter = new(player.transform.position.x, crouchTop);
        RaycastHit2D hitLeft = Physics2D.Raycast(originCenter + Vector2.left * halfWidth, Vector2.up, headroomNeeded, obstacleMask);
        RaycastHit2D hitCenter = Physics2D.Raycast(originCenter, Vector2.up, headroomNeeded, obstacleMask);
        RaycastHit2D hitRight = Physics2D.Raycast(originCenter + Vector2.right * halfWidth, Vector2.up, headroomNeeded, obstacleMask);

        if ((hitLeft.collider != null && !hitLeft.collider.isTrigger) ||
            (hitCenter.collider != null && !hitCenter.collider.isTrigger) ||
            (hitRight.collider != null && !hitRight.collider.isTrigger))
        {
            Debug.Log($"Cannot stand: hit '{hitCenter.collider?.name}' at distance {hitCenter.distance}/{headroomNeeded}");
            return false; // Остаёмся в crouch
        }

        // Места хватает — встаём
        capsule.size = originalCapsuleSize;
        capsule.offset = originalCapsuleOffset;
        animator.SetBool("Crouching", false);
        Debug.Log("Stood up successfully");
        return true;
    }
    
}