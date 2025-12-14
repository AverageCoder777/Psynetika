using UnityEngine;
using System.Collections;
public class CrouchingState : GroundedState
{
    BoxCollider2D capsule;
    Vector2 originalCapsuleSize;
    Vector2 originalCapsuleOffset;
    private LayerMask obstacleMask = LayerMask.GetMask("Walls"); // Слой препятствий
    private float headCheckDistanceBuffer = 0.05f;
    private bool crouchHeld;
    private bool jumpInput;

    public CrouchingState(Player player, StateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();
        capsule = player.GetComponent<BoxCollider2D>();
        originalCapsuleSize = capsule.size;
        originalCapsuleOffset = capsule.offset;
        if (player.DebugMessages) Debug.Log("Entered Crouching State");
        Vector2 newSize = new(originalCapsuleSize.x, originalCapsuleSize.y * player.CrouchHeightMultiplier);
        float delta = originalCapsuleSize.y - newSize.y;
        capsule.size = newSize;
        capsule.offset = new Vector2(originalCapsuleOffset.x, originalCapsuleOffset.y - delta / 2f);
        animator.SetBool("Crouching", true);
    }
    public override void HandleInput()
    {
        base.HandleInput();
        crouchHeld = player.PlayerInput.actions["Crouch"].IsPressed();
        jumpInput = player.PlayerInput.actions["Jump"].WasPressedThisFrame();
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (!crouchHeld&&CanStandUp())
        {
            StopCrouch();
            stateMachine.ChangeState(player.IdleState);
        }
        if (crouchHeld && jumpInput)
        {
            StopCrouch();
            grounded = false;
            player.StartCoroutine(DropThroughPlatform());
            stateMachine.ChangeState(player.FallingState);
        }
    }
    public override void PhysicsUpdate()
    {
        // Base не применяем от дочернего класса, чтобы не было движения с полной скоростью
        player.Rb.linearVelocity = new Vector2(player.MovementInput.x * player.Speed * 0.5f, player.Rb.linearVelocity.y);
        if (player.MovementInput.x > 0.01f)
            player.ActiveSR.flipX = false;
        else if (player.MovementInput.x < -0.01f)
            player.ActiveSR.flipX = true;
    }
    public override void Exit()
    {
        base.Exit();
        animator.SetBool("Crouching", false);
        if (player.DebugMessages) Debug.Log("Exited Crouching State");
    }
    private bool CanStandUp()
    {
        Vector2 crouchCenter = (Vector2)player.transform.position + capsule.offset;
        float crouchTop = crouchCenter.y + (capsule.size.y / 2f) - headCheckDistanceBuffer;

        // Дистанция проверки = delta высоты + buffer
        float deltaHeight = originalCapsuleSize.y - capsule.size.y;
        float headroomNeeded = deltaHeight + headCheckDistanceBuffer;

        // 3 рэйкаста вверх: слева, центр, справа
        float halfWidth = capsule.size.x / 2f;
        Vector2 originCenter = new(player.transform.position.x, crouchTop);
        RaycastHit2D hitLeft = Physics2D.Raycast(originCenter + Vector2.left * halfWidth, Vector2.up, headroomNeeded, obstacleMask);
        RaycastHit2D hitCenter = Physics2D.Raycast(originCenter, Vector2.up, headroomNeeded, obstacleMask);
        RaycastHit2D hitRight = Physics2D.Raycast(originCenter + Vector2.right * halfWidth, Vector2.up, headroomNeeded, obstacleMask);

        if ((hitLeft.collider != null&&!hitLeft.collider.isTrigger) ||
            (hitCenter.collider != null&&!hitCenter.collider.isTrigger) ||
            (hitRight.collider != null&&!hitRight.collider.isTrigger))
        {
            if (player.DebugMessages) Debug.Log($"Cannot stand: hit '{hitCenter.collider?.name}' at distance {hitCenter.distance}/{headroomNeeded}");
            return false;
        }
        else
        {
            if (player.DebugMessages) Debug.Log("Can stand up: no obstacles detected above");
            return true;
        }
    }
    public void StopCrouch()
    {
        capsule.size = originalCapsuleSize;
        capsule.offset = originalCapsuleOffset;
        animator.SetBool("Crouching", false);
        if (player.DebugMessages) Debug.Log("Stood up successfully");
        return;
    }
    IEnumerator DropThroughPlatform()
    {
        int platformLayer = LayerMask.NameToLayer(player.PlatformLayerName);
        if (platformLayer == -1) yield break;

        int playerLayer = player.gameObject.layer;
        Physics2D.IgnoreLayerCollision(playerLayer, platformLayer, true);
        yield return new WaitForSeconds(player.DropThroughDuration);
        Physics2D.IgnoreLayerCollision(playerLayer, platformLayer, false);
    }
}