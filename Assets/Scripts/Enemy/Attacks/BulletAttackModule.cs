using System;
using UnityEngine;

// Выстрел простыми пулями (компонент Bullet). Для снарядов с логикой (ноды, эффекты при попадании)
// есть AbilityAttackModule + ProjectileDefinition.
[Serializable]
[AddTypeMenu("Дальний бой/Выстрел")]
public class BulletAttackModule : EnemyAttackModule
{
    [Tooltip("Префаб с компонентом Bullet. У пули врага targetLayers должен указывать на слой Player")]
    public GameObject bulletPrefab;

    [Min(0)] public int damage = 10;

    [Tooltip("Смещение точки спавна от центра врага; X зеркалится направлением взгляда")]
    public Vector2 spawnOffset = new Vector2(0.65f, 0.22f);

    [Tooltip("Сколько пуль за одну атаку")]
    [Min(1)] public int shots = 1;

    [Tooltip("Пауза между пулями очереди, сек")]
    [Min(0f)] public float shotInterval = 0.12f;

    public override bool CanUse(EnemyController controller) => bulletPrefab != null;

    public override void Execute(EnemyController controller)
    {
        controller.Attack.SpawnBullets(bulletPrefab, damage, spawnOffset, shots, shotInterval);
    }

    // Миграция старых конфигов, где стрельба описывалась полями bulletPrefab/bulletDamage.
    internal static BulletAttackModule FromLegacy(EnemyConfig config) => new()
    {
        label = "Выстрел (из устаревших полей)",
        animatorParameter = "Shoot",
        windup = config.attackDuration,
        bulletPrefab = config.bulletPrefab,
        damage = config.bulletDamage,
        spawnOffset = config.bulletSpawnOffset
    };
}
