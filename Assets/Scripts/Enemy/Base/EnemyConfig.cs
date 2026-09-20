using System.Collections.Generic;
using UnityEngine;

/*
Все данные типа врага в одном ассете: новый враг = новый конфиг + префаб, без кода.
Поведение задаётся данными:
  perception — как замечает игрока (триггеры или радиусы),
  patrol/ground — что делает, пока игрока нет,
  attacks — полиморфный список модулей атак (ближний бой, выстрел, каст способности, свои наследники).

Старые конфиги (где атака описывалась полями meleeDamage/bulletPrefab/abilities) продолжают работать:
если attacks пуст, ResolveAttacks() собирает модули из устаревших полей.
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
    public EnemyPatrolSettings patrol = new();
    public EnemyGroundSettings ground = new();

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

    [Header("Лут")]
    [Min(0)] public int coinsToDrop = 0;
    public GameObject coinPrefab;

    [Header("Статус-эффекты")]
    public StatusEffectConfig statusEffects;

    [Header("Смерть")]
    [Min(0f)] public float deathDespawnDelay = 0.7f;

    [Header("Устаревшее — читается, только пока список Attacks пуст")]
    [Min(0)] public int meleeDamage = 10;
    [Tooltip("Время замаха: урон/выстрел происходит в конце цикла")]
    [Min(0.05f)] public float attackDuration = 2f;
    public GameObject bulletPrefab;
    [Min(0)] public int bulletDamage = 10;
    public Vector2 bulletSpawnOffset = new Vector2(0.65f, 0.22f);
    public List<AbilityDefinition> abilities = new();

    private List<EnemyAttackModule> resolvedAttacks;

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
    }
}
