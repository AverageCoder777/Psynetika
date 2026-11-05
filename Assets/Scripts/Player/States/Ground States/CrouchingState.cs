using UnityEngine;
using System.Collections;
public class CrouchingState : GroundedStates
{
    private static readonly int CrouchingHash = Animator.StringToHash("Crouching");
    private static readonly int CrouchShootingHash = Animator.StringToHash("CrouchShooting");
    BoxCollider2D capsule;
    Vector2 originalCapsuleSize;
    Vector2 originalCapsuleOffset;
    private LayerMask obstacleMask = LayerMask.GetMask("Up Walls");
    private bool crouchHeld;
    private bool jumpInput;
    private bool dropCompleted;
    private bool isDropping = false;
    private bool shootInput;
    private float nextShotTime;

    public CrouchingState(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings) : base(player, stateMachine, settings) { }

    public override void Enter()
    {
        base.Enter();
        dropCompleted = false;
        isDropping = false;
        capsule = player.GetComponent<BoxCollider2D>();
        originalCapsuleSize = capsule.size;
        originalCapsuleOffset = capsule.offset;
        float newHeight = originalCapsuleSize.y * settings.crouch.crouchHeightMultiplier;
        float delta = originalCapsuleSize.y - newHeight;
        capsule.size = new Vector2(originalCapsuleSize.x, newHeight);
        capsule.offset = new Vector2(originalCapsuleOffset.x, originalCapsuleOffset.y - delta / 2f);
        charManager.ActiveAnimator.SetBool(CrouchingHash, true);
    }
    public override void HandleInput()
    {
        base.HandleInput();
        crouchHeld = movement.PlayerInput.actions["Crouch"].IsPressed();
        jumpInput = movement.PlayerInput.actions["Jump"].WasPressedThisFrame();
        shootInput = movement.PlayerInput.actions["Attack"].WasPressedThisFrame();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
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
            stateMachine.ChangeState(player.FlyingState);
            return;
        }
        if (shootInput && stateMachine.CurrentPlayerState == this)
        {
            TryCrouchShot();
        }
    }
    public override void PhysicsUpdate()
    {
        float targetX = movementInput.x * movement.GetCurrentSpeed() * settings.crouch.crouchSpeedMultiplier;
        float currentX = movement.Rb.linearVelocity.x;
        float accel = Mathf.Abs(movementInput.x) > settings.detection.movementInputThreshold ? settings.move.accelerationRate : settings.move.frictionRate;
        float newX = Mathf.Lerp(currentX, targetX, accel * Time.fixedDeltaTime);

        movement.Rb.linearVelocity = new Vector2(newX, movement.Rb.linearVelocity.y);

        if (movementInput.x > settings.detection.movementInputThreshold)
            charManager.ActiveSR.flipX = false;
        else if (movementInput.x < -settings.detection.movementInputThreshold)
            charManager.ActiveSR.flipX = true;
    }
    public override void Exit()
    {
        base.Exit();
        capsule.size = originalCapsuleSize;
        capsule.offset = originalCapsuleOffset;
        if (player.debugMessages) Debug.Log("Stood up successfully");
        charManager.ActiveAnimator.SetBool(CrouchingHash, false);
    }
    // Нижний выстрел Сатаны: не вставая, пуля летит низко и попадает по мелочи вроде нейрончика.
    // Перезарядка — то же время, что у выстрела стоя.
    private void TryCrouchShot()
    {
        if (charManager.GetCurrentCharacterType() != PlayerCharacterType.Satan || Time.time < nextShotTime)
        {
            return;
        }

        nextShotTime = Time.time + attack.GetCurrentAttackStat<float>(AttackStatId.AttackSpeed);
        float dir = charManager.ActiveSR.flipX ? -1f : 1f;
        Bounds bounds = capsule.bounds;
        Vector2 spawnPos = new(
            bounds.center.x + dir * settings.combat.bulletSpawnOffsetX,
            bounds.min.y + bounds.size.y * settings.crouch.shotHeight);
        attack.FireBullet(spawnPos, dir);

        Animator animator = charManager.ActiveAnimator;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == CrouchShootingHash)
            {
                animator.SetTrigger(CrouchShootingHash);
                break;
            }
        }
    }

    private bool CanStandUp()
    {
        Vector2 capsuleCenter = (Vector2)player.transform.position + capsule.offset;
        float halfWidth = capsule.size.x / 2f;
        float heightDifference = originalCapsuleSize.y - capsule.size.y;
        float headroomNeeded = heightDifference + settings.detection.headCheckDistanceBuffer;

        Vector2 origin = new(capsuleCenter.x, capsuleCenter.y + capsule.size.y / 2f);
        RaycastHit2D hitCenter = Physics2D.Raycast(origin, Vector2.up, headroomNeeded, obstacleMask);
        RaycastHit2D hitLeft = Physics2D.Raycast(origin + Vector2.left * halfWidth, Vector2.up, headroomNeeded, obstacleMask);
        RaycastHit2D hitRight = Physics2D.Raycast(origin + Vector2.right * halfWidth, Vector2.up, headroomNeeded, obstacleMask);

        return !((hitCenter.collider != null && !hitCenter.collider.isTrigger) ||
                 (hitLeft.collider != null && !hitLeft.collider.isTrigger) ||
                 (hitRight.collider != null && !hitRight.collider.isTrigger));
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
        
        yield return new WaitForSeconds(settings.detection.dropThroughDuration);

        foreach (var platformCollider in platformColliders)
        {
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }
        
        dropCompleted = true;
        isDropping = false;
    }
}