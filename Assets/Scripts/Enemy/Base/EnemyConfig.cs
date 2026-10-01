using System.Collections.Generic;
using UnityEngine;

/*
Все данные типа врага в одном ассете: новый враг = новый конфиг + префаб, без кода.
Поведение задаётся данными:
  body       — «тело»: как враг физически двигается (ходьба, полёт…),
  idle       — тактика, пока игрок не замечен (стоять, патруль…),
  engage     — тактика в бою между атаками (преследование…),
  home       — поводок и возвращение к точке спавна,
  perception — как замечает игрока (триггеры или радиусы),
  attacks    — полиморфный список модулей атак (ближний бой, выстрел, каст способности, свои наследники),
  squad      — командные атаки по очереди,
  deathEffects — что происходит с телом после смерти (взрыв и т.п.).

Старые конфиги продолжают работать: пустые body/idle/engage собираются из устаревших блоков
ground/patrol, пустой attacks — из полей meleeDamage/bulletPrefab/abilities.
*/
[CreateAssetMenu(menuName = "Psynetika/Enemy Config", fileName = "EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    public enum AttackSelection
    {
        Priority = 0,
        Random = 1
    }

    [Header("Здоровье")]
    [Min(1)] public int maxHp = 100;

    [Header("Движение")]
    [Min(0f)] public float moveSpeed = 2f;

    [Tooltip("Тело врага упирается в игрока и толкает его. Выключено = проходят друг сквозь друга (зоны агро и удара работают как прежде)")]
    public bool blocksPlayer = false;

    [Tooltip("Как враг двигается. Пусто = ходьба с настройками из устаревшего блока Ground")]
    [SerializeReference, SubclassSelector]
    public EnemyLocomotion body;

    [Tooltip("Что делает, пока игрок не замечен. Пусто = патруль или стояние по устаревшему блоку Patrol")]
    [SerializeReference, SubclassSelector]
    public EnemyTactic idle;

    [Tooltip("Как двигается в бою между атаками. Пусто = преследование")]
    [SerializeReference, SubclassSelector]
    public EnemyTactic engage;

    public EnemyHomeSettings home = new();

    [Header("Обнаружение игрока")]
    public EnemyPerceptionSettings perception = new();

    [Header("Атаки")]
    [Tooltip("Пусто = модули собираются из устаревших полей внизу конфига")]
    [SerializeReference, SubclassSelector]
    public List<EnemyAttackModule> attacks = new();

    [Tooltip("Priority — первая подходящая из списка, Random — случайная из подходящих")]
    public AttackSelection attackSelection = AttackSelection.Priority;

    [Tooltip("Базовый множитель скорости атаки: 2 = атакует вдвое быстрее, 0.5 = вдвое медленнее")]
    [Min(0.05f)] public float attackSpeed = 1f;

    [Tooltip("Общая пауза после любой атаки, сек")]
    [Min(0f)] public float attackCooldown = 0f;

    public EnemySquadSettings squad = new();

    [Header("Лут")]
    [Tooltip("Монеты, выпадающие при смерти. Пустой префаб = без лута")]
    public CoinDrop loot = new();

    [Header("Статус-эффекты")]
    public StatusEffectConfig statusEffects;

    [Header("Смерть")]
    [Tooltip("Тело исчезает не раньше этого времени, а эффекты смерти могут продлить его жизнь")]
    [Min(0f)] public float deathDespawnDelay = 0.7f;

    [Tooltip("Что происходит после смерти: взрыв тела и т.п.")]
    [SerializeReference, SubclassSelector]
    public List<EnemyDeathEffect> deathEffects = new();

    [Header("Устаревшее — читается, только пока Idle/Body пусты")]
    public EnemyPatrolSettings patrol = new();
    public EnemyGroundSettings ground = new();

    [Header("Устаревшее — читается, только пока список Attacks пуст")]
    [Min(0)] public int meleeDamage = 10;
    [Tooltip("Время замаха: урон/выстрел происходит в конце цикла")]
    [Min(0.05f)] public float attackDuration = 2f;
    public GameObject bulletPrefab;
    [Min(0)] public int bulletDamage = 10;
    public Vector2 bulletSpawnOffset = new Vector2(0.65f, 0.22f);
    public List<AbilityDefinition> abilities = new();

    private List<EnemyAttackModule> resolvedAttacks;
    private EnemyLocomotion legacyBody;
    private EnemyTactic legacyIdle;
    private EnemyTactic legacyEngage;

    // Модули движения: настроенные в конфиге или собранные из устаревших блоков.
    // Возвращаются настройки, а не рантайм — рантайм каждый враг создаёт себе сам (CreateRuntime).
    public EnemyLocomotion ResolveBody() => body ?? (legacyBody ??= GroundLocomotion.FromLegacy(ground));

    public EnemyTactic ResolveIdleTactic() => idle ?? (legacyIdle ??= patrol != null && patrol.enabled
        ? PatrolLineTactic.FromLegacy(patrol)
        : new StandTactic());

    public EnemyTactic ResolveEngageTactic() => engage ?? (legacyEngage ??= new ChaseTactic());

    // Итоговый набор атак: либо то, что настроено в attacks, либо миграция устаревших полей.
    // Результат кешируется — конфиг общий для всех экземпляров врага.
    public IReadOnlyList<EnemyAttackModule> ResolveAttacks()
    {
        if (resolvedAttacks != null)
        {
            return resolvedAttacks;
        }

        resolvedAttacks = new List<EnemyAttackModule>();

        if (attacks != null)
        {
            foreach (EnemyAttackModule module in attacks)
            {
                if (module != null)
                {
                    resolvedAttacks.Add(module);
                }
            }
        }

        if (resolvedAttacks.Count > 0)
        {
            return resolvedAttacks;
        }

        // Миграция «всё или ничего»: устаревшие поля читаются только у конфигов без настроенных атак.
        if (bulletPrefab != null)
        {
            resolvedAttacks.Add(BulletAttackModule.FromLegacy(this));
        }
        else if (meleeDamage > 0)
        {
            resolvedAttacks.Add(MeleeAttackModule.FromLegacy(this));
        }

        if (abilities != null)
        {
            foreach (AbilityDefinition definition in abilities)
            {
                if (definition != null)
                {
                    resolvedAttacks.Add(AbilityAttackModule.FromLegacy(definition, this));
                }
            }
        }

        return resolvedAttacks;
    }

    private void OnValidate()
    {
        // Правки в инспекторе (в том числе в плей-моде) должны попадать во врагов сразу.
        resolvedAttacks = null;
        legacyBody = null;
        legacyIdle = null;
        legacyEngage = null;
    }
}
