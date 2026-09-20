using System.Collections.Generic;
using UnityEngine;

//Скрипты
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyAttack))]
[RequireComponent(typeof(EnemySensor))]
[RequireComponent(typeof(StatusEffectHandler))]

/*
Тонкий координатор врага (зеркало PlayerController): владеет ссылками на компоненты
и стейт-машиной, логика живёт в компонентах и состояниях.

Минимальный враг = GameObject + EnemyController + ссылка на EnemyConfig: недостающие
компоненты контроллер доставляет сам. Архетип (ближник, стрелок, патрульный, кастер)
задаётся данными в конфиге, наследник нужен только ради нестандартных состояний —
тогда переопредели CreateStates()/InitialStateId.
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

    private readonly Dictionary<EnemyStateId, EnemyStates> states = new();

    public EnemyStateId CurrentStateId { get; private set; } = EnemyStateId.Idle;

    protected virtual EnemyStateId InitialStateId =>
        config != null && config.patrol != null && config.patrol.enabled ? EnemyStateId.Patrol : EnemyStateId.Idle;
    #endregion

    private HashSet<int> animatorParameters;

    protected virtual void Awake()
    {
        Animator = GetComponent<Animator>();
        CacheAnimatorParameters();

        // Порядок важен: каждый компонент доискивает соседей в своём Initialize,
        // поэтому здесь достаточно, чтобы к моменту добавления StatusEffectHandler всё уже существовало.
        Health = GetOrAddComponent<EnemyHealth>();
        Movement = GetOrAddComponent<EnemyMovement>();
        Attack = GetOrAddComponent<EnemyAttack>();
        Sensor = GetOrAddComponent<EnemySensor>();
        StatusEffectHandler statusHandler = GetOrAddComponent<StatusEffectHandler>();

        if (config == null)
        {
            Debug.LogError($"[EnemyController] {name}: 'config' не назначен. Создай ассет через Create → Psynetika → Enemy Config и положи ссылку в поле Config на префабе. Враг отключён.");
            enabled = false;
            return;
        }

        Health.Initialize(config);
        Movement.Initialize(config);
        Attack.Initialize(config);
        Sensor.Initialize(config);
        statusHandler.SetConfigIfEmpty(config.statusEffects);
        if (TryGetComponent(out EnemyLoot loot))
        {
            loot.Initialize(config);
        }

        StateMachine = new EnemyStateMachine();
        CreateStates();
        Health.Died += OnDied;
    }

    protected virtual void Start()
    {
        EnemyStates initial = GetState(InitialStateId) ?? GetState(EnemyStateId.Idle);
        if (initial == null)
        {
            Debug.LogError($"[EnemyController] {name}: не зарегистрировано ни одного состояния.");
            enabled = false;
            return;
        }

        CurrentStateId = InitialStateId;
        StateMachine.Initialize(initial);
    }

    protected virtual void Update()
    {
        EnemyStates state = StateMachine?.CurrentEnemyState;
        if (state == null) return;

        state.HandleInput();
        state.LogicUpdate();
    }

    protected virtual void FixedUpdate()
    {
        StateMachine?.CurrentEnemyState?.PhysicsUpdate();
    }

    private void OnDestroy()
    {
        if (Health != null)
        {
            Health.Died -= OnDied;
        }
    }

    #region States
    // Реестр состояний по ролям: новое поведение = зарегистрировать состояние на роль,
    // переходы в остальных состояниях менять не нужно.
    protected virtual void CreateStates()
    {
        RegisterState(EnemyStateId.Idle, new EnemyIdleState(this, StateMachine));
        RegisterState(EnemyStateId.Patrol, new EnemyPatrolState(this, StateMachine));
        RegisterState(EnemyStateId.Follow, new EnemyFollowState(this, StateMachine));
        RegisterState(EnemyStateId.Attack, new EnemyAttackState(this, StateMachine));
        RegisterState(EnemyStateId.Dead, new EnemyDeadState(this, StateMachine));
    }

    public void RegisterState(EnemyStateId id, EnemyStates state)
    {
        if (state == null) return;
        states[id] = state;
    }

    public EnemyStates GetState(EnemyStateId id) => states.TryGetValue(id, out EnemyStates state) ? state : null;

    public bool HasState(EnemyStateId id) => states.ContainsKey(id);

    // Переход по роли. Возвращает false, если роль не зарегистрирована — вызывающее состояние остаётся текущим.
    public bool ChangeState(EnemyStateId id)
    {
        EnemyStates next = GetState(id);
        if (next == null)
        {
            Debug.LogWarning($"[EnemyController] {name}: состояние '{id}' не зарегистрировано.");
            return false;
        }

        CurrentStateId = id;
        StateMachine.ChangeState(next);
        return true;
    }
    #endregion

    #region Animator
    // Аниматор врага может быть неполным (новый префаб, временные ассеты) — обращения к
    // отсутствующим параметрам молча игнорируются вместо спама предупреждений Unity.
    public void SetAnimatorTrigger(int hash)
    {
        if (HasAnimatorParameter(hash)) Animator.SetTrigger(hash);
    }

    public void SetAnimatorBool(int hash, bool value)
    {
        if (HasAnimatorParameter(hash)) Animator.SetBool(hash, value);
    }

    public bool HasAnimatorParameter(int hash) =>
        Animator != null && animatorParameters != null && animatorParameters.Contains(hash);

    private void CacheAnimatorParameters()
    {
        animatorParameters = new HashSet<int>();
        if (Animator == null || Animator.runtimeAnimatorController == null) return;

        foreach (AnimatorControllerParameter parameter in Animator.parameters)
        {
            animatorParameters.Add(parameter.nameHash);
        }
    }
    #endregion

    protected virtual void OnDied()
    {
        Health.Died -= OnDied;

        if (StateMachine == null || StateMachine.CurrentEnemyState == null)
        {
            // Смерть до инициализации стейт-машины: минимальная развязка без состояний.
            SetAnimatorTrigger(DieHash);
            Sensor.DisableSensing();
            enabled = false;
            Destroy(gameObject, config != null ? config.deathDespawnDelay : 0f);
            return;
        }

        ChangeState(EnemyStateId.Dead);
    }

    private T GetOrAddComponent<T>() where T : Component
    {
        return TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
    }
}
