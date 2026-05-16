using UnityEngine;
using System.Collections;
public class CrouchingState : GroundedStates
{
    private static readonly int CrouchingHash = Animator.StringToHash("Crouching");
    BoxCollider2D capsule;
    Vector2 originalCapsuleSize;
    Vector2 originalCapsuleOffset;
    private LayerMask obstacleMask = LayerMask.GetMask("Up Walls"); // Слой препятствий
    private readonly float headCheckDistanceBuffer = 0.1f;
    private bool crouchHeld;
    private bool attackInput;
    private bool jumpInput;
    private bool dropCompleted;
    private bool isDropping = false; // Флаг для предотвращения множественных запусков

    public CrouchingState(Player player, StateMovMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();
        dropCompleted = false;
        isDropping = false; // Сбрасываем флаг при входе в состояние
        capsule = player.GetComponent<BoxCollider2D>();
        originalCapsuleSize = capsule.size;
        originalCapsuleOffset = capsule.offset;
        Vector2 newSize = new(originalCapsuleSize.x, originalCapsuleSize.y * player.CrouchHeightMultiplier);
        float delta = originalCapsuleSize.y - newSize.y;
        capsule.size = newSize;
        capsule.offset = new Vector2(originalCapsuleOffset.x, originalCapsuleOffset.y - delta / 2f);
        Animator.SetBool(CrouchingHash, true);
        player.LastState = this;
    }
    public override void HandleInput()
    {
        base.HandleInput();
        attackInput = player.PlayerInput.actions["Attack"].WasPressedThisFrame();
        crouchHeld = player.PlayerInput.actions["Crouch"].IsPressed();
        jumpInput = player.PlayerInput.actions["Jump"].WasPressedThisFrame();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        Debug.Log("Player nazhimaet hod :" + movementInput.x);
        if (!crouchHeld && CanStandUp())
        {
            stateMachine.ChangeState(player.IdleState);
        }
        if (crouchHeld && jumpInput && !isDropping)
        {
            grounded = false;
            isDropping = true;
            player.StartCoroutine(DropThroughPlatform());
        }
        if (dropCompleted)
        {
            stateMachine.ChangeState(player.AirState);
        }
        if (attackInput && player.GetCurrentCharState() == player.SatanState)
        {
            stateMachine.ChangeState(player.HittingState);
        }
    }
    public override void PhysicsUpdate()
    {
        float targetX = movementInput.x * player.GetCharSpeed() * 0.5f;
        float currentX = player.Rb.linearVelocity.x;

        float accel = Mathf.Abs(movementInput.x) > 0.001f ? player.AccelerationRate : player.FrictionRate;
        float newX = Mathf.Lerp(currentX, targetX, accel * Time.fixedDeltaTime);

        player.Rb.linearVelocity = new Vector2(newX, player.Rb.linearVelocity.y);

        if (movementInput.x > 0.001f)
            player.ActiveSR.flipX = false;
        else if (movementInput.x < -0.001f)
            player.ActiveSR.flipX = true;
    }
    public override void Exit()
    {
        base.Exit();
        capsule.size = originalCapsuleSize;
        capsule.offset = originalCapsuleOffset;
        if (player.DebugMessages) Debug.Log("Stood up successfully");
        Animator.SetBool(CrouchingHash, false);
    }
    private bool CanStandUp()
    {
        Vector2 capsuleCenter = (Vector2)player.transform.position + capsule.offset;

        float crouchCapsuleTop = capsuleCenter.y + (capsule.size.y / 1.5f);

        float originalCapsuleTop = capsuleCenter.y + (originalCapsuleSize.y / 1.5f);

        float headroomNeeded = originalCapsuleTop - crouchCapsuleTop + headCheckDistanceBuffer;

        float halfWidth = capsule.size.x / 2f;
        Vector2 originCenter = new(capsuleCenter.x, crouchCapsuleTop);

        RaycastHit2D hitLeft = Physics2D.Raycast(originCenter + Vector2.left * halfWidth, Vector2.up, headroomNeeded, obstacleMask);
        RaycastHit2D hitCenter = Physics2D.Raycast(originCenter, Vector2.up, headroomNeeded, obstacleMask);
        RaycastHit2D hitRight = Physics2D.Raycast(originCenter + Vector2.right * halfWidth, Vector2.up, headroomNeeded, obstacleMask);

        if ((hitLeft.collider != null && !hitLeft.collider.isTrigger) ||
            (hitCenter.collider != null && !hitCenter.collider.isTrigger) ||
            (hitRight.collider != null && !hitRight.collider.isTrigger))
        {
            return false;
        }
        else
        {
            return true;
        }
    }
    IEnumerator DropThroughPlatform()
    {
        dropCompleted = false;
        int platformLayer = LayerMask.NameToLayer("Platform");
        
        if (platformLayer == -1) 
        {
            Debug.LogError("Platform слой не найден!");
            isDropping = false;
            yield break;
        }
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        
        Collider2D[] platformColliders = Physics2D.OverlapBoxAll(
            playerCollider.bounds.center,
            playerCollider.bounds.size,
            0,
            1 << platformLayer
        );

        foreach (var platformCollider in platformColliders)
        {
            Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
        }
        
        yield return new WaitForSeconds(player.DropThroughDuration);

        foreach (var platformCollider in platformColliders)
        {
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }
        
        dropCompleted = true;
        isDropping = false;
    }
}