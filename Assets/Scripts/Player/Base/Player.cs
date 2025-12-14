using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    #region Fields
    [Header("Персонажи")]
    public GameObject satan;
    public GameObject sobaka;
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
    [Header("Удары Собаки")]
    [Tooltip("Скорость удара задает время на один удар")]
    [SerializeField] private float hittingSpeedSobaka = 1f;
    [SerializeField] private float hitDistanceSobaka = 1f;
    [SerializeField] private int hittingDamageSobaka = 10;
    [Header("Стрельба Сатаны")]
    public GameObject bulletPrefab;
    [SerializeField] private int hittingDamageSatana = 22;
    [Tooltip("Скорость удара задает время на один выстрел")]
    [SerializeField] private float hittingSpeedSatana = 2f;
    [SerializeField] private float hitDistanceSatana = 2f;
    [SerializeField] private bool debugMessages = false;
    Rigidbody2D rb;
    private PlayerInput playerInput;
    #endregion
    #region Publlic Properties
    public Rigidbody2D Rb { get { return rb; } }
    public Animator ActiveAnimator { get => activeAnimator; set => activeAnimator = value; }
    public SpriteRenderer ActiveSR { get => activeSR; set => activeSR = value; }
    public GameObject ActiveCharacter {get => activeCharacter; set => activeCharacter = value; }
    public GameObject Satan { get => satan; set => satan = value; }
    public GameObject Sobaka { get => sobaka; set => sobaka = value; }
    public Vector2 MovementInput { get => movementInput; set => movementInput = value; }
    public PlayerInput PlayerInput => playerInput;
    public string PlatformLayerName => platformLayerName;
    public float DropThroughDuration => dropThroughDuration;
    public float Speed => speed;
    public float Thrust => thrust;
    public float MaxDoubleJumpHeight => maxDoubleJumpHeight;
    public float RollDistance => rollDistance;
    public float RollDuration => rollDuration;
    public float CrouchHeightMultiplier => CROUCH_HEIGHT_MULTIPLIER;
    public float SwitchDelay => switchDelay;
    public bool DebugMessages =>debugMessages;
    public bool CharacterIsSatan()
    {
        if (ActiveCharacter == Satan)
        {
            return true;
        }
        else
        {
            return false;
        }    
    }
    public float GetHittingSpeed()
    {
        if (ActiveCharacter == Satan)
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
        if (ActiveCharacter == Satan)
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
        if (ActiveCharacter == Satan)
        {
            return hittingDamageSatana;
        }
        else
        {
            return hittingDamageSobaka;
        }
    }

    #endregion
    #region Unity MonoBehaviour Callbacks
    void Awake()
    {

        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        satan = transform.GetChild(0).gameObject;
        sobaka = transform.GetChild(1).gameObject;

        activeCharacter = satan;
        satan.SetActive(true);
        sobaka.SetActive(false);
        
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
        if (debugMessages) Debug.Log("Player took " + damage + " damage. Current HP: " + hp);
        activeAnimator.SetTrigger("Hurt");
        UpdateHealthUI();
        if (hp <= 0) Die();
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