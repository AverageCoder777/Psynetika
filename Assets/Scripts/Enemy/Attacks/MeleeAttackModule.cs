using System;
using UnityEngine;

// Ближний удар по текущей цели сенсора.
[Serializable]
[AddTypeMenu("Ближний бой/Удар")]
public class MeleeAttackModule : EnemyAttackModule
{
    [Min(0)] public int damage = 10;
    public DamageType damageType = DamageType.Physical;

    public override void Execute(EnemyController controller)
    {
        controller.Attack.DealMeleeDamage(ResolveTarget(controller), damage, damageType);
    }

    // Миграция старых конфигов, где ближний бой описывался полями meleeDamage/attackDuration.
    internal static MeleeAttackModule FromLegacy(EnemyConfig config) => new()
    {
        label = "Ближний удар (из устаревших полей)",
        animatorParameter = "Hit",
        windup = config.attackDuration,
        damage = config.meleeDamage
    };
}
