using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Персонажи")]
    public GameObject characterA;
    public GameObject characterB;
    private GameObject activeCharacter;
    private Animator activeAnimator;
    private SpriteRenderer activeSR;

    [Header("Движение")]
    [SerializeField] float speed = 5f;
    private Vector2 movementInput = Vector2.zero;

    [Header("Прыжок")]
    [SerializeField] float thrust = 12f;
    [SerializeField] float doubleJumpThrust = 8f;
    [SerializeField] float maxDoubleJumpHeight = 40f;
    bool grounded = true;
    bool firstJump = false;
    bool canDoubleJump = false;
    bool doubleJump = false;

    [Header("Приседание")]
    float CROUCH_HEIGHT_MULTIPLIER = 0.5f;
    bool isCrouching = false;
    bool crouchRequested = false;

    [Header("Рывок/кувырок")]
    [SerializeField] float rollDistance = 4f;
    [SerializeField] float rollDuration = 0.25f;
    bool isRolling = false;
    bool rollRequested = false;

    [Header("Здоровье и его UI")]
    [SerializeField] UIScript ui;
    [SerializeField] UnityEngine.UI.Image healthBar;
    [SerializeField] int maxHp = 100;
    [SerializeField] int hp = 100;

    private bool isSwitching = false;
    public float switchDelay = 0.5f;

    [Header("Прыжок через платформу вниз")]
    [Tooltip("Имя слоя для платформ")]
    [SerializeField] private string platformLayerName = "Platform";
    [Tooltip("Время, в течение которого игрок не будет сталкиваться с платформами при провале")]
    [SerializeField] private float dropThroughDuration = 0.5f;
    Rigidbody2D rb;

    public PlayerInput playerInput;

    #region Publlic Properties
    public Rigidbody2D Rb { get { return rb; } }
    public Animator GetActiveAnimator() => activeAnimator;
    public SpriteRenderer GetActiveSpriteRenderer() => activeSR;
    public Vector2 MovementInput { get => movementInput; set => movementInput = value; }
    public bool FirstJump { get => firstJump; set => firstJump = value; }
    public bool DoubleJump { get => doubleJump; set => doubleJump = value; }
    public bool CanDoubleJump { get => canDoubleJump; set => canDoubleJump = value; }
    public bool Grounded { get => grounded; set => grounded = value; }
    public bool IsRolling { get => isRolling; set => isRolling = value; }
    public bool IsCrouching { get => isCrouching; set => isCrouching = value; }
    public bool IsSwitching { get => isSwitching; set => isSwitching = value; }
    public bool CrouchRequested { get => crouchRequested; set => crouchRequested = value; }
    public float Speed => speed;
    public float Thrust => thrust;
    public float DoubleJumpThrust => doubleJumpThrust;
    public float MaxDoubleJumpHeight => maxDoubleJumpHeight;
    public float RollDistance => rollDistance;
    public float RollDuration => rollDuration;
    public bool RollRequested { get => rollRequested; set => rollRequested = value; }
    public float CrouchHeightMultiplier => CROUCH_HEIGHT_MULTIPLIER;

    #endregion
    #region Unity MonoBehaviour Callbacks
    void Awake()
    {

        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        characterA = transform.GetChild(0).gameObject;
        characterB = transform.GetChild(1).gameObject;

        activeCharacter = characterA;
        characterA.SetActive(true);
        characterB.SetActive(false);

        CacheActiveVisuals();

        UpdateHealthUI();
    }
    void Start()
    {
        playerSM = new PlayerStateMachine();
        IdleState = new PlayerIdleState(this, playerSM);
        JumpingState = new PlayerJumpingState(this, playerSM);
        CrouchingState = new PlayerCrouchingState(this, playerSM);
        RollingState = new PlayerRollingState(this, playerSM);
        FallingState = new PlayerFallingState(this, playerSM);
        playerSM.Initialize(IdleState);

    }
    void Update()
    {
        playerSM.CurrentPlayerState.HandleInput();
        playerSM.CurrentPlayerState.LogicUpdate();
    }
    void FixedUpdate()
    {
        playerSM.CurrentPlayerState.PhysicsUpdate();
        activeAnimator.SetFloat("Velocity", rb.linearVelocity.y);
        activeAnimator.SetBool("Grounded", grounded);
    }
    void CacheActiveVisuals()
    {
        activeAnimator = activeCharacter.GetComponent<Animator>();
        activeSR = activeCharacter.GetComponent<SpriteRenderer>();
    }
    #endregion
    void OnTriggerEnter2D(Collider2D ground)
    {
        if (ground.CompareTag("Floor") || ground.CompareTag("Platform"))
        {
            grounded = true;
            firstJump = false;
            canDoubleJump = false;
        }
    }
    void OnTriggerExit2D(Collider2D ground)
    {
        if (rb.linearVelocity.y != 0)
        {
            grounded = false;
        }
    }
    #region IEnumerators
    public IEnumerator SwitchCharacter()
    {
        isSwitching = true;

        if (activeAnimator != null) activeAnimator.SetTrigger("isSwitching");

        yield return new WaitForSeconds(switchDelay);

        if (activeCharacter == characterA && characterB != null)
        {
            if (characterA != null) characterA.SetActive(false);
            characterB.SetActive(true);
            activeCharacter = characterB;
        }
        else if (characterA != null)
        {
            if (characterB != null) characterB.SetActive(false);
            characterA.SetActive(true);
            activeCharacter = characterA;
        }

        CacheActiveVisuals();

        activeAnimator.SetBool("isSwitching", false);

        isSwitching = false;

        Debug.Log("Switched character");
    }

    //IEnumerator DropThroughPlatform()
    //{
    //    int platformLayer = LayerMask.NameToLayer(platformLayerName);
    //    if (platformLayer == -1) yield break;

    //    int playerLayer = gameObject.layer;
    //    Physics2D.IgnoreLayerCollision(playerLayer, platformLayer, true);
    //    yield return new WaitForSeconds(dropThroughDuration);
    //    Physics2D.IgnoreLayerCollision(playerLayer, platformLayer, false);
    //}
    #endregion
    #region Abilities functions
    public bool TryStartRoll()
    {
        if (!grounded || isCrouching || isRolling) return false;
        return true;
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;
        if (hp < 0) hp = 0;
        Debug.Log("Player took " + damage + " damage. Current HP: " + hp);
        activeAnimator.SetTrigger("Hurt");
        UpdateHealthUI();
        if (hp < 0) Die();
    }

    public void Heal(int amount)
    {
        hp += amount;
        if (hp > maxHp) hp = maxHp;
        UpdateHealthUI();
    }

    void UpdateHealthUI()
    {
        if (healthBar != null) healthBar.fillAmount = (float)hp / maxHp;
    }

    public void ResetHealth()
    {
        hp = maxHp;
        UpdateHealthUI();
    }

    public void Die()
    {
        ui.GameOver();
    }
    #endregion
    #region Animation Triggers
    private void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {
        playerSM.CurrentPlayerState.AnimationTriggerEvent(triggerType);

    }
    public enum AnimationTriggerType
    {
        Roll,
        Jump,
        DoubleJump,
        Crouch,
        StopCrouch
    }
    #endregion
    #region State Machine Variables
    public PlayerStateMachine playerSM { get; set; }
    public PlayerGroundedState GroundedState { get; set; }
    public PlayerIdleState IdleState { get; set; }
    public PlayerJumpingState JumpingState { get; set; }
    public PlayerCrouchingState CrouchingState { get; set; }
    public PlayerRollingState RollingState { get; set; }
    public PlayerFallingState FallingState { get; set; }

    #endregion
}