using UnityEngine;
using UnityEngine.InputSystem;
//Компоненты юнити
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]

//Скрипт
[RequireComponent(typeof(PlayerCharacterManager))]
public class PlayerMoving : MonoBehaviour, IMovable, IJumpable
{
    private PlayerStaticSettings settings;
    private PlayerDynSettings status;
    public float Speed => GetCurrentSpeed();
    public Vector2 MovementInput {get => movementInput ; set => movementInput = value;}
    private PlayerInput playerInput;
    private PlayerCharacterManager charManager;
    private Vector2 movementInput;
    private Rigidbody2D rb;
    private int airJumpsRemaining = 2;
    private InputAction jumpAction;

    // Состояния игрока — отдельные объекты, поэтому общее для них «чувство прыжка» живёт здесь.
    private float lastJumpPressTime = float.NegativeInfinity;
    private float lastGroundedTime = float.NegativeInfinity;
    private bool jumpCutAvailable;

    public PlayerInput PlayerInput => playerInput;
    public Rigidbody2D Rb => rb;

    public void ResetAirJumps() => airJumpsRemaining = settings.jump.maxAirJumpsCount;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        charManager = GetComponent<PlayerCharacterManager>();
        status = charManager.GetComponent<PlayerDynSettings>();
        rb = GetComponent<Rigidbody2D>();
        settings = Resources.Load<PlayerStaticSettings>("PlayerDefaultSettings");
        airJumpsRemaining = settings.jump.maxAirJumpsCount;
        jumpAction = playerInput.actions["Jump"];
    }

    private void Update()
    {
        if (jumpAction != null && jumpAction.WasPressedThisFrame())
        {
            lastJumpPressTime = Time.time;
        }
    }

    public float GetCurrentSpeed()
    {
        return charManager.GetCurrentCharacterType() == PlayerCharacterType.Satan ?
        settings.move.satanSpeed * status.speedMultiplier :
        settings.move.dogSpeed * status.speedMultiplier;
    }
    public float GetJumpForce()
    {
        return settings.jump.thrust;
    }
    public bool TryConsumeAirJump()
    {
        if (airJumpsRemaining <= 0)
        {
            return false;
        }

        airJumpsRemaining--;
        return true;
    }

    #region Jump feel
    public bool JumpPressedThisFrame => jumpAction != null && jumpAction.WasPressedThisFrame();
    public bool JumpHeld => jumpAction != null && jumpAction.IsPressed();

    // Нажатие этого кадра или недавнее, ещё не потраченное (буфер прыжка).
    public bool HasBufferedJump => JumpPressedThisFrame || Time.time - lastJumpPressTime <= settings.jump.jumpBufferTime;
    public void ConsumeBufferedJump() => lastJumpPressTime = float.NegativeInfinity;

    // Вызывается, пока герой стоит на земле. Coyote time отсчитывается от последнего вызова.
    public void MarkGrounded() => lastGroundedTime = Time.time;
    public bool InCoyoteTime => Time.time - lastGroundedTime <= settings.jump.coyoteTime;

    // Любой прыжок (с земли, в воздухе, от стены) — сбрасывает буфер и coyote, чтобы не прыгнуть дважды.
    public void NotifyJumped(bool allowJumpCut)
    {
        ConsumeBufferedJump();
        lastGroundedTime = float.NegativeInfinity;
        jumpCutAvailable = allowJumpCut;
    }

    // Переменная высота прыжка: отпустил кнопку на подъёме — подъём обрезается. Срабатывает раз за прыжок.
    public void TryCutJump()
    {
        if (!jumpCutAvailable)
        {
            return;
        }
        Vector2 v = rb.linearVelocity;
        if (v.y <= 0f)
        {
            jumpCutAvailable = false;
            return;
        }
        if (jumpAction != null && !jumpAction.IsPressed())
        {
            v.y *= settings.jump.jumpCutMultiplier;
            rb.linearVelocity = v;
            jumpCutAvailable = false;
        }
    }

    public void NotifyLanded()
    {
        jumpCutAvailable = false;
    }
    #endregion
}
