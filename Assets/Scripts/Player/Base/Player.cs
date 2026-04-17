    using System;
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
    [SerializeField] float speed = 5f; // базовая скорость игрока
    [SerializeField] float accelerationRate = 15f; //ускорение, с которым игрок начинает движение
    [SerializeField] float frictionRate = 20f; // трение, которое замедляет персонажа, если игрок перестал идти
    private Vector2 movementInput = Vector2.zero;

    [Header("Прыжок")]
    [SerializeField] float thrust = 12f; // ускорение, придаваемое персонажу при прыжке
    [SerializeField] float doubleJumpThrust = 6f; // ускорение двойного прыжка
    [SerializeField] float upGravityScale = 1.1f; // множитель гравитации в первой половине параболы прыжка
    [SerializeField] float downGravityScale = 2f; // множитель гравитации во второй половине параболы прыжка

    [Header("Цепление за стену")]
    [SerializeField] float wallSlideSpeed = 1f; //скорость соскальзывания по стене
    [SerializeField] float wallJumpForce = 10f; //сила прыжка от стены
    [SerializeField] float wallDetectionDistance = 0.5f; //Расстояние, на котором стена обнаруживается
    [SerializeField] float wallWaitTime = 0.2f; //Время, которое игрок должен провести на стене, чтобы можно было отпрыгнуть от неё

    [Header("Приседание")]
    float CROUCH_HEIGHT_MULTIPLIER = 0.5f; //Модификатор, который умножает высоту коллайдера при приседании

    [Header("Рывок/кувырок")]
    [SerializeField] float rollDistance = 4f; //расстояние рывка/кувырка
    [SerializeField] float rollDuration = 0.25f; //время рывка/кувырка

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
    [SerializeField] private float dropThroughDuration = 0.5f; //время, на которое платформы перестают иметь коллизию для персонажа
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
    [Header("Spells")]
    [SerializeField] private SpellController spellController;
    [Header("Interaction")]
    [SerializeField] private InteractionDetector interactionDetector;
    private State lastState;
#if UNITY_EDITOR
    [SerializeField] private bool debugMessages = false;
#endif
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
    public float AccelerationRate => accelerationRate;
    public float FrictionRate => frictionRate;
    public float Thrust => thrust;
    public float DoubleJumpThrust => doubleJumpThrust;
    public float UpGravityScale => upGravityScale;
    public float DownGravityScale => downGravityScale;
    public float WallSlideSpeed => wallSlideSpeed;
    public float WallJumpForce => wallJumpForce;
    public float WallDetectionDistance => wallDetectionDistance;
    public float WallWaitTime => wallWaitTime;
    public float RollDistance => rollDistance;
    public float RollDuration => rollDuration;
    public float CrouchHeightMultiplier => CROUCH_HEIGHT_MULTIPLIER;
    public float SwitchDelay => switchDelay;
    public State LastState { get => lastState; set => lastState = value; }
    public bool DebugMessages =>debugMessages;
    public SpellController SpellController => spellController;
    private InputAction openUI;
    private InputAction InteractAction;
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
        openUI = playerInput.actions["Pause"];
        InteractAction = playerInput.actions["Interact"];
        
        if (spellController == null)
        {
            spellController = GetComponent<SpellController>();
        }

        if (interactionDetector == null)
        {
            interactionDetector = GetComponentInChildren<InteractionDetector>();
        }
        
        satan = transform.GetChild(0).gameObject;
        sobaka = transform.GetChild(1).gameObject;

        activeCharacter = satan;
        satan.SetActive(true);
        sobaka.SetActive(false);
        
        activeAnimator = activeCharacter.GetComponent<Animator>();
        activeSR = activeCharacter.GetComponent<SpriteRenderer>();

        UpdateHealthUI();
    }

    private void OnEnable()
    {
        if (InteractAction != null)
        {
            InteractAction.performed += OnInteractPerformed;
        }
    }

    private void OnDisable()
    {
        if (InteractAction != null)
        {
            InteractAction.performed -= OnInteractPerformed;
        }
    }

    void Start()
    {
        playerSM = new StateMachine();
        IdleState = new IdleState(this, playerSM);
        JumpingState = new JumpingState(this, playerSM);
        CrouchingState = new CrouchingState(this, playerSM);
        RollingState = new RollingState(this, playerSM);
        AirState = new AirState(this, playerSM);
        SwitchState = new SwitchState(this, playerSM);
        HittingState = new HittingState(this, playerSM);
        SpellCastState = new SpellCastState(this, playerSM);
        WallState = new WallState(this, playerSM);
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

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        TryInteract();
    }

    public void TryInteract()
    {
        if (interactionDetector == null)
        {
            return;
        }

        IInteractable interactable = interactionDetector.GetClosestInteractable(transform.position);
        interactable?.Interact();
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
    #region State Machine Variables
    public StateMachine playerSM { get; set; }
    public IdleState IdleState { get; set; }
    public JumpingState JumpingState { get; set; }
    public CrouchingState CrouchingState { get; set; }
    public RollingState RollingState { get; set; }
    public AirState AirState { get; set; }
    public SwitchState SwitchState { get; set; }
    public HittingState HittingState { get; set; }
    public SpellCastState SpellCastState { get; set; }
    public WallState WallState { get; set; }

    #endregion
}
