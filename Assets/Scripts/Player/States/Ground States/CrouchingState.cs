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
        player.MovementInput = player.PlayerInput.actions["Move"].ReadValue<Vector2>();
        crouchHeld = player.PlayerInput.actions["Crouch"].IsPressed();
        jumpInput = player.PlayerInput.actions["Jump"].WasPressedThisFrame();
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (!crouchHeld && CanStandUp())
        {
            stateMachine.ChangeState(player.IdleState);
        }
        if (crouchHeld && jumpInput && !isDropping) // Проверяем флаг
        {
            grounded = false;
            isDropping = true; // Устанавливаем флаг
            player.StartCoroutine(DropThroughPlatform());
        }
        if (dropCompleted)
        {
            stateMachine.ChangeState(player.AirState);
        }
    }
    public override void PhysicsUpdate()
    {
        float xmovement = player.MovementInput.x*player.GetCharSpeed()*0.5f;
        player.Rb.linearVelocity = new Vector2(xmovement, player.Rb.linearVelocity.y);

        if (player.MovementInput.x > 0.001f)
            player.ActiveSR.flipX = false;
        else if (player.MovementInput.x < -0.001f)
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

        // Верхняя точка текущей (сжатой) капсул
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
    IEnumerator DropThroughPlatform()
    {
        dropCompleted = false;
        
        // Находим все коллайдеры платформ под игроком
        int platformLayer = LayerMask.NameToLayer("Platform");
        
        if (platformLayer == -1) 
        {
            Debug.LogError("Platform слой не найден!");
            isDropping = false;
            yield break;
        }
        
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        
        // Ищем коллайдеры платформ, которые соприкасаются с игроком
        Collider2D[] platformColliders = Physics2D.OverlapBoxAll(
            playerCollider.bounds.center,
            playerCollider.bounds.size,
            0,
            1 << platformLayer
        );
        
        Debug.Log($"Найдено платформ коллайдеров: {platformColliders.Length}");
        
        // Отключаем коллизии между игроком и найденными платформами
        foreach (var platformCollider in platformColliders)
        {
            Debug.Log($"Игнорируем коллизию между игроком и платформой: {platformCollider.gameObject.name}");
            Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
        }
        
        yield return new WaitForSeconds(player.DropThroughDuration);
        
        // Восстанавливаем коллизии
        foreach (var platformCollider in platformColliders)
        {
            Debug.Log($"Восстанавливаем коллизию между игроком и платформой: {platformCollider.gameObject.name}");
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }
        
        dropCompleted = true;
        isDropping = false;
        Debug.Log("Прыжок через платформу совершен, dropCompleted = " + dropCompleted);
    }
}