using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    #region Fields
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
    [SerializeField] float maxDoubleJumpHeight = 40f;

    [Header("Приседание")]
    float CROUCH_HEIGHT_MULTIPLIER = 0.5f;

    [Header("Рывок/кувырок")]
    [SerializeField] float rollDistance = 4f;
    [SerializeField] float rollDuration = 0.25f;

    [Header("Здоровье и его UI")]
    [SerializeField] UIScript ui;
    [SerializeField] UnityEngine.UI.Image healthBar;
    [SerializeField] int maxHp = 100;
    [SerializeField] int hp = 100;

    [Header("Время смены персонажа")]
    [SerializeField] public float switchDelay = 0.5f;

    [Header("Прыжок через платформу вниз")]
    [Tooltip("Имя слоя для платформ")]
    [SerializeField] private string platformLayerName = "Platform";
    [Tooltip("Время, в течение которого игрок не будет сталкиваться с платформами при провале")]
    [SerializeField] private float dropThroughDuration = 0.5f;
    [Header("Удар")]
    [Tooltip("Скорость удара задает время на один удар")]
    [SerializeField] private float hittingSpeedSobaka = 1f;
    [Tooltip("Скорость удара задает время на один удар")]
    [SerializeField] private float hittingSpeedSatana = 2f;
    [Tooltip("Дистанция удара")]
    [SerializeField] private float hitDistanceSobaka = 1f;
    [Tooltip("Дистанция удара")]
    [SerializeField] private float hitDistanceSatana = 2f;
    [Tooltip("Урон от удара")]
    [SerializeField] private int hittingDamageSobaka = 10;
    [Tooltip("Урон от удара")]
    [SerializeField] private int hittingDamageSatana = 22;
    Rigidbody2D rb;
    public PlayerInput playerInput;
    #endregion
    #region Publlic Properties
    public Rigidbody2D Rb { get { return rb; } }
    public Animator ActiveAnimator { get => activeAnimator; set => activeAnimator = value; }
    public SpriteRenderer ActiveSR { get => activeSR; set => activeSR = value; }
    public GameObject ActiveCharacter {get => activeCharacter; set => activeCharacter = value; }
    public GameObject CharacterA { get => characterA; set => characterA = value; }
    public GameObject CharacterB { get => characterB; set => characterB = value; }
    public Vector2 MovementInput { get => movementInput; set => movementInput = value; }
    public string PlatformLayerName => platformLayerName;
    public float DropThroughDuration => dropThroughDuration;
    public float Speed => speed;
    public float Thrust => thrust;
    public float MaxDoubleJumpHeight => maxDoubleJumpHeight;
    public float RollDistance => rollDistance;
    public float RollDuration => rollDuration;
    public float CrouchHeightMultiplier => CROUCH_HEIGHT_MULTIPLIER;
    public float SwitchDelay => switchDelay;
    public float GetHittingSpeed()
    {
        if (ActiveCharacter == CharacterA)
        {
            return hittingSpeedSatana;
        }
        else
        {
            return hittingSpeedSobaka;
        }
    }

    public float GetHitDistance()
    {
        if (ActiveCharacter == CharacterA)
        {
            return hitDistanceSatana;
        }
        else
        {
            return hitDistanceSobaka;
        }
    }
    public int GetHittingDamage()
    {
        if (ActiveCharacter == CharacterA)
        {
            return hittingDamageSobaka;
        }
        else
        {
            return hittingDamageSatana;
        }
    }

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
        
        activeAnimator = activeCharacter.GetComponent<Animator>();
        activeSR = activeCharacter.GetComponent<SpriteRenderer>();

        UpdateHealthUI();
    }
    void Start()
    {
        playerSM = new StateMachine();
        IdleState = new IdleState(this, playerSM);
        JumpingState = new JumpingState(this, playerSM);
        CrouchingState = new CrouchingState(this, playerSM);
        RollingState = new RollingState(this, playerSM);
        FallingState = new FallingState(this, playerSM);
        SwitchState = new SwitchState(this, playerSM);
        HittingState = new HittingState(this, playerSM);
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
    }
    #endregion
    #region Abilities functions

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
    public StateMachine playerSM { get; set; }
    public GroundedState GroundedState { get; set; }
    public IdleState IdleState { get; set; }
    public JumpingState JumpingState { get; set; }
    public CrouchingState CrouchingState { get; set; }
    public RollingState RollingState { get; set; }
    public FallingState FallingState { get; set; }
    public SwitchState SwitchState { get; set; }
    public HittingState HittingState { get; set; }

    #endregion
}