using UnityEngine;
using System;

//Скрипты
[RequireComponent(typeof(PlayerCharacterManager))]
[RequireComponent(typeof(PlayerActions))]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerMoving))]
[RequireComponent(typeof(SpellController))]
[RequireComponent(typeof(PlayerAttack))]

/*
Скрипт лишь владеет ссылками на кирпичики, из которых строится вся работа игрока. 
Сюда не стоит прямо реализовывать определенную логику, а лишь делегировать её для какого-то другого класса.
*/
public class PlayerController : MonoBehaviour
{
    #region Fields
    private static readonly int VelocityHash = Animator.StringToHash("Velocity");
    private PlayerMoving moving;
    private PlayerAttack attack;
    private PlayerHealth health;
    public float switchDelay = 0.5f;
    private SpellController spellController;
    private InteractionDetector interactionDetector;
    private State lastState;
    public bool debugMessages = false;
    private PlayerActions playerActions;
    private PlayerCharacterManager playerCharManager;
    private bool isVisibleToEnemies = true;
    private PlayerCharacterType activePlayerCharacterType;

    #endregion
    #region Publlic Fields
    public event Action<PlayerCharacterType> ActiveCharacterChanged;
    public bool IsVisibleToEnemies => isVisibleToEnemies;
    public float SwitchDelay => switchDelay;
    public State LastState { get => lastState; set => lastState = value; }
    public SpellController SpellController => spellController;
    public PlayerMoving Movement => moving;
    public PlayerAttack Attack => attack;
    public PlayerCharacterManager PlayerCharManager { get => playerCharManager; set => playerCharManager = value; }
    public PlayerCharacterType GetCurrentCharacterType()
    {
        return activePlayerCharacterType;
    }
    public void SetCurrentCharacterType(PlayerCharacterType type)
    {
        if (activePlayerCharacterType == type) return;
        activePlayerCharacterType = type;
        ActiveCharacterChanged?.Invoke(type);
    }
    #endregion
    #region Unity MonoBehaviour Callbacks
    void Awake()
    {
        playerActions = GetComponent<PlayerActions>();
        moving = GetComponent<PlayerMoving>();
        attack = GetComponent<PlayerAttack>();
        health = GetComponent<PlayerHealth>();
        spellController = GetComponent<SpellController>();
        interactionDetector = GetComponentInChildren<InteractionDetector>();
        playerCharManager = GetComponent<PlayerCharacterManager>();
        activePlayerCharacterType = playerCharManager.ActiveCharacter == playerCharManager.Satan ? PlayerCharacterType.Satan : PlayerCharacterType.Dog;

        PlayerSM = new StateMachine();
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
        playerCharManager.ActiveAnimator.SetFloat(VelocityHash, moving.Rb.linearVelocity.y);
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
