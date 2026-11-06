using System.Collections.Generic;
using UnityEngine;

/*
Мини-босс «Мама паук». Наследник контроллера ради нестандартных режимов:
  Rest     — SpiderMotherDormantState: ждёт на стартовой точке стены, пока игрок не войдёт в арену;
  Engage   — SpiderMotherWallState: сидит на точке стены и плюётся паутиной и ядом (модули из config.attacks);
  Relocate — SpiderMotherCrawlState: ползёт по маршруту арены на другую точку или на землю;
  Summon   — SpiderMotherSummonState: на земле под щитом-паутиной рожает микро-паучков;
  Stagger  — SpiderMotherStunnedState: щит разбит — стан, затем обратно на стену;
  Dead     — обычная смерть: тело падает, оставшиеся паучки гибнут вместе с ней.

Тело — CrawlLocomotion (без гравитации), геометрия — SpiderMotherArena, фазы — SpiderMotherConfig.
Контроллер держит общее для режимов: где паучиха (Spot), её позу (прижата к стене или стоит),
счётчик точек до спуска, щит и рождённых паучков.
*/
public class SpiderMotherController : EnemyController
{
    private static readonly int OnWallHash = Animator.StringToHash("OnWall");

    [Tooltip("Арена босса. Пусто = ищется среди родителей")]
    [SerializeField] private SpiderMotherArena arena;

    private readonly List<EnemyHealth> minions = new();
    private SpiderMotherCrawlState crawl;
    private SpiderMotherStunnedState stunned;
    private int perchesLeft;

    public SpiderMotherConfig Boss { get; private set; }
    public SpiderMotherArena Arena => arena;
    public SpiderWebShield Shield { get; private set; }
    public bool FightStarted { get; private set; }

    // Где паучиха сейчас (обновляется по прибытии; во время переползания — откуда ползёт).
    public SpiderSpot Spot { get; set; } = SpiderSpot.Ground;

    public int AliveMinions
    {
        get
        {
            minions.RemoveAll(m => m == null || !m.IsAlive);
            return minions.Count;
        }
    }

    protected override void Awake()
    {
        Boss = Config as SpiderMotherConfig;
        if (arena == null) arena = GetComponentInParent<SpiderMotherArena>();
        Shield = GetComponentInChildren<SpiderWebShield>(true);
        if (Shield != null) Shield.Drop();
        base.Awake();
    }

    protected override void CreateStates()
    {
        if (Boss == null || arena == null)
        {
            Debug.LogError($"[SpiderMotherController] {name}: нужен конфиг типа SpiderMotherConfig и SpiderMotherArena среди родителей (или в поле Arena). Босс ведёт себя как обычный враг.");
            base.CreateStates();
            return;
        }

        crawl = new SpiderMotherCrawlState(this, StateMachine);
        stunned = new SpiderMotherStunnedState(this, StateMachine);

        RegisterState(EnemyStateId.Rest, new SpiderMotherDormantState(this, StateMachine));
        RegisterState(EnemyStateId.Engage, new SpiderMotherWallState(this, StateMachine));
        RegisterState(EnemyStateId.Relocate, crawl);
        RegisterState(EnemyStateId.Summon, new SpiderMotherSummonState(this, StateMachine));
        RegisterState(EnemyStateId.Stagger, stunned);
        RegisterState(EnemyStateId.Dead, new EnemyDeadState(this, StateMachine));
    }

    #region Ход боя
    // Арена: игрок вошёл, бой начался.
    public void BeginFight()
    {
        if (FightStarted) return;
        FightStarted = true;
        perchesLeft = Boss != null ? Boss.wallPhase.RollPerches() : 1;
    }

    // Атаки с точки кончились: на другую точку или, если точки до спуска кончились, на землю.
    public void LeavePerch()
    {
        perchesLeft--;
        if (perchesLeft <= 0)
        {
            crawl.Begin(SpiderSpot.Ground, EnemyStateId.Summon);
            return;
        }
        crawl.Begin(arena.RandomPerch(Spot, Boss.wallPhase.otherWallChance), EnemyStateId.Engage);
    }

    // С земли (после стана или по таймауту щита) — на случайную стену, новый круг.
    public void ClimbToWall()
    {
        perchesLeft = Boss.wallPhase.RollPerches();
        crawl.Begin(arena.RandomPerch(Spot, 1f), EnemyStateId.Engage);
    }

    public void Stun() => stunned.Begin(Boss.stun.duration, Boss.stun.animatorParameter);
    #endregion

    #region Поза
    /*
    Прижаться к стене side (+1 — правая, -1 — левая) или встать на землю (0).
    На стене тело повёрнуто на 90° лапами к стене: коллайдер поворачивается вместе со спрайтом.
    */
    public void SetPose(int side)
    {
        float angle = side * 90f;
        Movement.Body.rotation = angle;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        SetAnimatorBool(OnWallHash, side != 0);
    }

    public void SnapTo(Vector2 position)
    {
        Movement.Body.position = position;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    public void FacePlayer()
    {
        Transform player = Sensor.PlayerTransform;
        if (player != null) Movement.Face(player.position.x - transform.position.x);
    }
    #endregion

    #region Паучки
    public bool CanSpawnMinion => Boss.groundPhase.minionPrefab != null && AliveMinions < Boss.groundPhase.maxAliveMinions;

    public void SpawnMinion()
    {
        SpiderMotherGroundPhase ground = Boss.groundPhase;
        if (ground.minionPrefab == null) return;

        Vector2 spread = ground.spawnSpread;
        Vector2 position = Movement.Body.position + new Vector2(Random.Range(-spread.x, spread.x), Random.Range(0f, spread.y));
        GameObject minion = Instantiate(ground.minionPrefab, position, Quaternion.identity, arena.transform);
        if (minion.TryGetComponent(out EnemyHealth minionHealth))
        {
            minions.Add(minionHealth);
        }
    }

    // Мать погибла — выводок гибнет вместе с ней (со своими эффектами смерти).
    private void KillMinions()
    {
        foreach (EnemyHealth minion in minions)
        {
            if (minion == null || !minion.IsAlive) continue;
            minion.ApplyDamage(new DamageEvent { Target = minion, Amount = minion.CurrentHp, Type = DamageType.Physical });
        }
        minions.Clear();
    }
    #endregion

    protected override void OnDied()
    {
        if (Shield != null) Shield.Drop();
        Health.IsInvulnerable = false;
        base.OnDied();
        KillMinions();

        // Тело отваливается от стены и падает на пол.
        if (Movement != null && Movement.Body != null)
        {
            SetPose(0);
            Movement.Body.bodyType = RigidbodyType2D.Dynamic;
            Movement.Body.gravityScale = 2f;
            Movement.Body.freezeRotation = true;
        }
    }
}
