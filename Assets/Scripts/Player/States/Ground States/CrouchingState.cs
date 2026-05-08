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
    private bool jumpInput;

    public CrouchingState(Player player, StateMovMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();
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
        player.MovementInput = player.PlayerInput.actions["Move"].ReadValue<Vector2>();
        crouchHeld = player.PlayerInput.actions["Crouch"].IsPressed();
        jumpInput = player.PlayerInput.actions["Jump"].WasPressedThisFrame();
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (!crouchHeld && CanStandUp())
        {
            StopCrouch();
            stateMachine.ChangeState(player.IdleState);
        }
        if (crouchHeld && jumpInput)
        {
            StopCrouch();
            grounded = false;
            player.StartCoroutine(DropThroughPlatform());
            stateMachine.ChangeState(player.AirState);
        }
    }
    public override void PhysicsUpdate()
    {
        player.Rb.linearVelocity = new Vector2(player.MovementInput.x, player.MovementInput.y);

        if (player.MovementInput.x > 0.001f)
            player.ActiveSR.flipX = false;
        else if (player.MovementInput.x < -0.001f)
            player.ActiveSR.flipX = true;
    }
    public override void Exit()
    {
        base.Exit();
        Animator.SetBool(CrouchingHash, false);
    }
    private bool CanStandUp()
    {
        Vector2 capsuleCenter = (Vector2)player.transform.position + capsule.offset;

        // Верхняя точка текущей (сжатой) капсулы
        float crouchCapsuleTop = capsuleCenter.y + (capsule.size.y / 1.5f);

        // Верхняя точка оригинальной капсулы
        float originalCapsuleTop = capsuleCenter.y + (originalCapsuleSize.y / 1.5f);

        // Дистанция проверки = разница в высоте + buffer
        float headroomNeeded = originalCapsuleTop - crouchCapsuleTop + headCheckDistanceBuffer;

        // 3 рэйкаста вверх: слева, центр, справа
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
    public void StopCrouch()
    {
        capsule.size = originalCapsuleSize;
        capsule.offset = originalCapsuleOffset;
        Animator.SetBool(CrouchingHash, false);
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