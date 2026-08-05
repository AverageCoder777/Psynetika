using UnityEngine;

//Скрипты
[RequireComponent(typeof(PlayerDynSettings))]
[RequireComponent(typeof(PlayerCharacterManager))]
[RequireComponent(typeof(PlayerActions))]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerMoving))]
[RequireComponent(typeof(PlayerAttack))]
[RequireComponent(typeof(SpellController))]

/*
Скрипт лишь владеет ссылками на кирпичики, из которых строится вся работа игрока. 
Сюда не стоит прямо реализовывать определенную логику, а лишь делегировать её для какого-то другого класса.
*/
public class PlayerController : MonoBehaviour
{
    #region Fields
    private static readonly int VelocityHash = Animator.StringToHash("Velocity");
    private PlayerMoving movement;
    private PlayerAttack attack;
    private PlayerHealth health;
    private PlayerStaticSettings settings;
    private PlayerDynSettings status;
    private SpellController spellController;
    private InteractionDetector interactionDetector;
    private State lastState;
    public bool debugMessages = false;
    private PlayerActions playerActions;
    private PlayerCharacterManager playerCharManager;
    private bool isVisibleToEnemies = true;

    #endregion
    #region Publlic Fields
    public bool IsVisibleToEnemies => isVisibleToEnemies;
    public State LastState { get => lastState; set => lastState = value; }
    public SpellController SpellController => spellController;
    public PlayerMoving Movement => movement;
    public PlayerAttack Attack => attack;
    public PlayerDynSettings Status => status;
    public PlayerCharacterManager PlayerCharManager { get => playerCharManager; set => playerCharManager = value; }
    #endregion
    #region Unity MonoBehaviour Callbacks
    void Awake()
    {
        settings = Resources.Load<PlayerStaticSettings>("PlayerDefaultSettings");
        status = GetComponent<PlayerDynSettings>();
        playerActions = GetComponent<PlayerActions>();
        movement = GetComponent<PlayerMoving>();
        attack = GetComponent<PlayerAttack>();
        health = GetComponent<PlayerHealth>();
        spellController = GetComponent<SpellController>();
        interactionDetector = GetComponentInChildren<InteractionDetector>();
        playerCharManager = GetComponent<PlayerCharacterManager>();

        PlayerSM = new StateMachine();
        new PlayerStateFactory().InitializeAllStates(this, PlayerSM, settings);
        PlayerSM.Initialize(IdleState);
    }

    private void OnEnable()
    {
        if (playerActions != null)
        {
            playerActions.OnInteract += OnInteractPerformed;
        }
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        if (playerActions != null)
        {
            playerActions.OnInteract -= OnInteractPerformed;
        }
        health.Died -= OnDied;
    }

    void Update()
    {
        PlayerSM.CurrentPlayerState.HandleInput();
        PlayerSM.CurrentPlayerState.LogicUpdate();
    }
    void FixedUpdate()
    {
        PlayerSM.CurrentPlayerState.PhysicsUpdate();
        playerCharManager.ActiveAnimator.SetFloat(VelocityHash, movement.Rb.linearVelocity.y);
    }

    private void OnDied(PlayerCharacterType type)
    {
        PlayerSM.ChangeState(DyingState);
    }

    private void OnInteractPerformed()
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

    public void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder") && PlayerSM.CurrentPlayerState == LadderState)
        {
            PlayerSM.ChangeState(FlyingState);
        }
    }

    #endregion
    #region Abilities functions
    public void DisableEnemyVisibility()
    {
        isVisibleToEnemies = false;
    }
    public void EnableEnemyVisibility()
    {
        isVisibleToEnemies = true;
    }
    #endregion
    #region State Machine Variables
    public StateMachine PlayerSM { get; set; }
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
    #endregion
}
