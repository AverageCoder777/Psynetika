using System.Collections.Generic;
using UnityEngine;

// Все статы типа врага в одном ассете: новый тип врага того же архетипа = новый конфиг без кода.
[CreateAssetMenu(menuName = "Psynetika/Enemy Config", fileName = "EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    [Header("Здоровье")]
    [Min(1)] public int maxHp = 100;

    [Header("Движение")]
    [Min(0f)] public float moveSpeed = 2f;

    [Header("Ближняя атака")]
    [Min(0)] public int meleeDamage = 10;
    [Tooltip("Время замаха/перезарядки: урон/выстрел происходит в конце цикла")]
    [Min(0.05f)] public float attackDuration = 2f;

    [Header("Дальний бой (для стрелков)")]
    [Tooltip("Префаб с компонентом Bullet; заполняется только для дальнобойных врагов")]
    public GameObject bulletPrefab;
    [Min(0)] public int bulletDamage = 10;
    [Tooltip("Смещение точки спавна пули от центра врага; X зеркалится направлением взгляда")]
    public Vector2 bulletSpawnOffset = new Vector2(0.65f, 0.22f);

    [Header("Лут")]
    [Min(0)] public int coinsToDrop = 0;
    public GameObject coinPrefab;

    [Header("Статус-эффекты")]
    public StatusEffectConfig statusEffects;

    [Header("Смерть")]
    [Min(0f)] public float deathDespawnDelay = 0.7f;

    [Header("Способности (для дальнобойных/боссов)")]
    public List<AbilityDefinition> abilities = new();
}
