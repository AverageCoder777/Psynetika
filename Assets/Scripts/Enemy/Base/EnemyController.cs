using UnityEngine;

//Скрипты
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyAttack))]
[RequireComponent(typeof(EnemySensor))]
[RequireComponent(typeof(StatusEffectHandler))]

/*
Тонкий координатор врага (зеркало PlayerController): владеет ссылками на компоненты
и стейт-машиной, логика живёт в компонентах и состояниях.
Новый архетип врага = подкласс с override CreateStates()/InitialState + свой EnemyConfig.
*/
public class EnemyController : MonoBehaviour
{
    private static readonly int DieHash = Animator.StringToHash("Die");

    [SerializeField] private EnemyConfig config;

    public EnemyConfig Config => config;
    public Animator Animator { get; private set; }
    public EnemyHealth Health { get; private set; }
    public EnemyMovement Movement { get; private set; }
    public EnemyAttack Attack { get; private set; }
    public EnemySensor Sensor { get; private set; }

    #region State Machine
    public EnemyStateMachine StateMachine { get; private set; }
    public EnemyStates IdleState { get; protected set; }
    public EnemyStates FollowState { get; protected set; }
    public EnemyStates AttackState { get; protected set; }
    protected virtual EnemyStates InitialState => IdleState;
    #endregion

    protected virtual void Awake()
    {
        Animator = GetComponent<Animator>();
        Health = GetComponent<EnemyHealth>();
        Movement = GetComponent<EnemyMovement>();
        Attack = GetComponent<EnemyAttack>();
        Sensor = GetComponent<EnemySensor>();

        if (config == null)
        {
            Debug.LogError($"[EnemyController] {name}: 'config' не назначен. Создай ассет через Create → Psynetika → Enemy Config и положи ссылку в поле Config на префабе. Враг отключён.");
            enabled = false;
            return;
        }

        Health.Initialize(config);
        Movement.Initialize(config);
        Attack.Initialize(config);
        if (TryGetComponent(out EnemyLoot loot))
        {
            loot.Initialize(config);
        }
        if (TryGetComponent(out StatusEffectHandler statusHandler))
        {
            statusHandler.SetConfigIfEmpty(config.statusEffects);
        }

        StateMachine = new EnemyStateMachine();
        CreateStates();
        Health.Died += OnDied;
    }

    protected virtual void Start()
    {
        StateMachine.Initialize(InitialState);
    }

    protected virtual void Update()
    {
        if (!Health.IsAlive) return;
        StateMachine.CurrentEnemyState.HandleInput();
        StateMachine.CurrentEnemyState.LogicUpdate();
    }

    protected virtual void FixedUpdate()
    {
        if (!Health.IsAlive) return;
        StateMachine.CurrentEnemyState.PhysicsUpdate();
    }

    private void OnDestroy()
    {
        if (Health != null)
        {
            Health.Died -= OnDied;
        }
    }

    protected virtual void CreateStates()
    {
        IdleState = new EnemyIdleState(this, StateMachine);
        FollowState = new EnemyFollowState(this, StateMachine);
        AttackState = new EnemyAttackState(this, StateMachine);
    }

    protected virtual void OnDied()
    {
        Animator?.SetTrigger(DieHash);
        Sensor.DisableSensing();
        enabled = false;
        Destroy(gameObject, config.deathDespawnDelay);
    }
}
