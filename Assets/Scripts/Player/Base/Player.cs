using System;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(SpellController))]
public class Player : MonoBehaviour, IAbilityCaster, IAbilityStatOwner, IAbilityDamageSource
{
    #region Fields
    [Header("Персонажи")]
    public GameObject satan;
    public GameObject dog;
    private GameObject activeCharacter;
    private Animator activeAnimator;
    private SpriteRenderer activeSR;

    [Header("Движение")]
    [SerializeField] float dogSpeed = 8f; // базовая скорость собаки
    [SerializeField] float satanSpeed = 6f; // базовая скорость сатаны
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
    [SerializeField] readonly float CROUCH_HEIGHT_MULTIPLIER = 0.7f; //Модификатор, который умножает высоту коллайдера при приседании

    [Header("Рывок/кувырок")]
    [SerializeField] float rollDistance = 4f; //расстояние рывка/кувырка
    [SerializeField] float rollDuration = 0.25f; //время рывка/кувырка

    [Header("Здоровье и его UI")]
    [SerializeField] UIScript ui;
    [SerializeField] UnityEngine.UI.Image healthBarSatan;
    [SerializeField] UnityEngine.UI.Image healthBarDog;
    [SerializeField] GameObject HPOverlaySatan;
    [SerializeField] GameObject HPOverlayDog;
    [SerializeField] int dogMaxHP = 100;
    [SerializeField] int satanMaxHP = 100;
    [SerializeField] int dogHP = 100;
    [SerializeField] int satanHP = 100;
    [Header("Время смены персонажа")]
    [SerializeField] public float switchDelay = 0.5f;

    [Header("Прыжок через платформу вниз")]
    [Tooltip("Имя слоя для платформ")]
    [SerializeField] private string platformLayerName = "Platform";
    [Tooltip("Время, в течение которого игрок не будет сталкиваться с платформами при провале")]
    [SerializeField] private float dropThroughDuration = 0.5f; //время, на которое платформы перестают иметь коллизию для персонажа
    [Header("Удары Собаки")]
    [Tooltip("Скорость удара задает время на один удар")]
    [SerializeField] private float dogHitTime = 1f;
    [SerializeField] private float dogHitDistance = 1f;
    [SerializeField] private int dogHitDamage = 10;
    [Header("Стрельба Сатаны")]
    public GameObject bulletPrefab;
    [SerializeField] private int satanHitDamage = 22;
    [Tooltip("Скорость удара задает время на один выстрел")]
    [SerializeField] private float satanHitTime = 2f;
    [SerializeField] private float satanHitDistance = 2f;
    [Header("Spells")]
    [SerializeField] private SpellController spellController;
    [Header("Interaction")]
    [SerializeField] private InteractionDetector interactionDetector;
    private State lastState;
    [SerializeField] private bool debugMessages = false;
    Rigidbody2D rb;
    private PlayerInput playerInput;
    private float speedMultiplier = 1f;
    private float attackSpeedMultiplier = 1f;
    private float damageMultiplier = 1f;
    private bool isVisibleToEnemies = true;
    #endregion
    #region Publlic Properties
    public Rigidbody2D Rb { get { return rb; } }
    public bool IsVisibleToEnemies => isVisibleToEnemies;
    public Animator ActiveAnimator { get => activeAnimator; set => activeAnimator = value; }
    public SpriteRenderer ActiveSR { get => activeSR; set => activeSR = value; }
    public GameObject ActiveCharacter { get => activeCharacter; set => activeCharacter = value; }
    public GameObject Satan { get => satan; set => satan = value; }
    public GameObject Dog { get => dog; set => dog = value; }
    public Vector2 MovementInput { get => movementInput; set => movementInput = value; }
    public PlayerInput PlayerInput => playerInput;
    public string PlatformLayerName => platformLayerName;
    public float DropThroughDuration => dropThroughDuration;
    public float GetCharSpeed()
    {
        float base_ = GetCurrentCharState() == SatanState ? satanSpeed : dogSpeed;
        return base_ * speedMultiplier;
    }
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
    public bool DebugMessages => debugMessages;
    public SpellController SpellController => spellController;
    public int MaxHp => GetMaxHp();
    public float SpeedMultiplier { get => speedMultiplier; set => speedMultiplier = value; }
    public float AttackSpeedMultiplier { get => attackSpeedMultiplier; set => attackSpeedMultiplier = value; }
    public float DamageMultiplier { get => damageMultiplier; set => damageMultiplier = value; }
    public bool SatanActive;
    public bool DogActive;
    private InputAction InteractAction;
    public PlayerState GetCurrentCharState()
    {
        return CharacterSM.GetCurrentState();
    }

    public float GetHitTime()
    {
        float baseHitTime;
        if (GetCurrentCharState() == SatanState)
        {
            baseHitTime = satanHitTime;
        }
        else
        {
            baseHitTime = dogHitTime;
        }

        float safeAttackSpeedMultiplier = Mathf.Max(0.05f, attackSpeedMultiplier);
        return baseHitTime / safeAttackSpeedMultiplier;
    }

    public float GetHitDistance()
    {
        if (GetCurrentCharState() == SatanState)
        {
            return satanHitDistance;
        }
        else
        {
            return dogHitDistance;
        }
    }
    public int GetHitDamage()
    {
        float base_ = GetCurrentCharState() == SatanState ? satanHitDamage : dogHitDamage;
        return Mathf.RoundToInt(base_ * damageMultiplier);
    }
    public int GetCharHP()
    {
        return GetCurrentCharState() == SatanState ? satanHP : dogHP;
    }
    public int GetMaxHp()
    {
        return GetCurrentCharState() == SatanState ? satanMaxHP : dogMaxHP;
    }

    // IAbilityCaster
    Transform IAbilityCaster.Transform => transform;
    Vector2 IAbilityCaster.Center =>
        TryGetComponent(out BoxCollider2D box) ? (Vector2)box.bounds.center : (Vector2)transform.position;
    float IAbilityCaster.FacingDirection => (activeSR != null && activeSR.flipX) ? -1f : 1f;
    Team IAbilityCaster.Team => global::Team.Player;
    MonoBehaviour IAbilityCaster.CoroutineHost => this;

    #endregion
    #region Unity MonoBehaviour Callbacks
    void Awake()
    {

        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        InteractAction = playerInput.actions["Interact"];

        if (spellController == null)
        {
            spellController = GetComponent<SpellController>();
        }

        if (interactionDetector == null)
        {
            interactionDetector = GetComponentInChildren<InteractionDetector>();
        }

        satan = transform.Find("Satan").gameObject;
        dog = transform.Find("Dog").gameObject;

        activeCharacter = satan;
        satan.SetActive(true);
        dog.SetActive(false);
        DogActive = true;
        SatanActive = true;

        activeAnimator = activeCharacter.GetComponent<Animator>();
        activeSR = activeCharacter.GetComponent<SpriteRenderer>();

        // Initialize state machines early to prevent null references
        PlayerSM = new StateMovMachine();
        CharacterSM = new StateCharMachine();
        IdleState = new IdleState(this, PlayerSM);
        JumpingState = new JumpingState(this, PlayerSM);
        CrouchingState = new CrouchingState(this, PlayerSM);
        RollingState = new RollingState(this, PlayerSM);
        FlyingState = new FlyingState(this, PlayerSM);
        SwitchState = new SwitchState(this, PlayerSM);
        HittingState = new HittingState(this, PlayerSM);
        WallState = new WallState(this, PlayerSM);
        LadderState = new LadderState(this, PlayerSM);
        SpellCastState = new SpellCastState(this, PlayerSM);
        DyingState = new DyingState(this, PlayerSM);
        DogState = new DogState(this, CharacterSM);
        SatanState = new SatanState(this, CharacterSM);
        PlayerSM.Initialize(IdleState);
        CharacterSM.Initialize(SatanState);

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
    void Update()
    {
        PlayerSM.CurrentPlayerState.HandleInput();
        PlayerSM.CurrentPlayerState.LogicUpdate();
        CharacterSM.CurrentPlayerState.LogicUpdate();
    }
    void FixedUpdate()
    {
        PlayerSM.CurrentPlayerState.PhysicsUpdate();
        CharacterSM.CurrentPlayerState.PhysicsUpdate();
        activeAnimator.SetFloat("Velocity", rb.linearVelocity.y);
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        TryInteract();
    }
    public void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder") && PlayerSM.CurrentPlayerState == LadderState)
        {
            PlayerSM.ChangeState(FlyingState);
        }
    }

    public void TryInteract()
    {
        if (DialogueManager.HasInstance && DialogueManager.Instance.IsDialogueActive)
        {
            return;
        }

        if (interactionDetector == null)
        {
            return;
        }

        IInteractable interactable = interactionDetector.GetClosestInteractable(transform.position);
        interactable?.Interact();
    }

    #endregion
    #region Abilities functions
    public int TryDrainHP(int amount, int minHp)
    {
        bool isSatan = GetCurrentCharState() == SatanState;
        int hp = isSatan ? satanHP : dogHP;
        if (hp <= minHp) return 0;
        int actual = Mathf.Min(amount, hp - minHp);
        if (isSatan) satanHP -= actual;
        else dogHP -= actual;
        UpdateHealthUI();
        return actual;
    }
    public void TakeDamage(int damage)
    {
        if (GetCurrentCharState() == SatanState)
        {
            satanHP -= damage;
            if (satanHP < 0) satanHP = 0;
        }
        else
        {
            dogHP -= damage;
            if (dogHP < 0) dogHP = 0;
        }
        if (debugMessages) Debug.Log("Player took " + damage + " damage. Current HP: " + GetCharHP() + ", max HP: " + GetMaxHp());
        activeAnimator.SetTrigger("Hurt");
        UpdateHealthUI();
        if (GetCharHP() <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (GetCurrentCharState() == SatanState)
        {
            satanHP += amount;
            if (satanHP > satanMaxHP) satanHP = satanMaxHP;
        }
        else
        {
            dogHP += amount;
            if (dogHP > dogMaxHP) dogHP = dogMaxHP;
        }
        UpdateHealthUI();
    }

    public void UpdateHealthUI()
    {
        bool isSatan = GetCurrentCharState() == SatanState;

        if (healthBarSatan != null && isSatan)
        {
            HPOverlaySatan.SetActive(true);
            HPOverlayDog.SetActive(false);
            healthBarSatan.fillAmount = (float)satanHP / satanMaxHP;
        }

        else if (healthBarDog != null)
        {
            HPOverlayDog.SetActive(true);
            HPOverlaySatan.SetActive(false);
            healthBarDog.fillAmount = (float)dogHP / dogMaxHP;
        }
    }
    public void ResetHealth()
    {
        if (GetCurrentCharState() == SatanState)
        {
            satanHP = satanMaxHP;
        }
        else
        {
            dogHP = dogMaxHP;
        }
        UpdateHealthUI();
    }

    public void Die()
    {
        PlayerSM.ChangeState(DyingState);
    }

    public void DisableEnemyVisibility()
    {
        isVisibleToEnemies = false;
    }

    int IAbilityStatOwner.CurrentHp => GetCharHP();
    int IAbilityDamageSource.GetBaseHitDamage() => GetHitDamage();

    public float GetStat(StatId stat)
    {
        switch (stat)
        {
            case StatId.AttackSpeed: return attackSpeedMultiplier;
            case StatId.Damage: return damageMultiplier;
            case StatId.MoveSpeed: return speedMultiplier;
            default:
                Debug.LogWarning($"[Player.GetStat] StatId {stat} is not supported.");
                return 1f;
        }
    }

    public void SetStat(StatId stat, float value)
    {
        switch (stat)
        {
            case StatId.AttackSpeed: attackSpeedMultiplier = value; break;
            case StatId.Damage: damageMultiplier = value; break;
            case StatId.MoveSpeed: speedMultiplier = value; break;
            default:
                Debug.LogWarning($"[Player.SetStat] StatId {stat} is not supported.");
                break;
        }
    }
    public void EnableEnemyVisibility()
    {
        isVisibleToEnemies = true;
    }
    #endregion
    #region State Machine Variables
    public StateMovMachine PlayerSM { get; set; }
    public StateCharMachine CharacterSM { get; set; }
    public IdleState IdleState { get; set; }
    public JumpingState JumpingState { get; set; }
    public CrouchingState CrouchingState { get; set; }
    public RollingState RollingState { get; set; }
    public FlyingState FlyingState { get; set; }
    public SwitchState SwitchState { get; set; }
    public HittingState HittingState { get; set; }
    public SpellCastState SpellCastState { get; set; }
    public SpellSlot PendingSpellSlot { get; set; }
    public WallState WallState { get; set; }
    public LadderState LadderState { get; set; }
    public DyingState DyingState { get; set; }
    public SatanState SatanState { get; set; }
    public DogState DogState { get; set; }
    #endregion
}
