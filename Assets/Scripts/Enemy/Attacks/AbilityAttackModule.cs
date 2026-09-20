using System;
using UnityEngine;

// Каст способности из системы Abilities: любой граф нод (снаряды, урон по площади, бафы, тики) становится атакой врага.
// Кулдаун и исполнение берёт на себя AbilityRunner, модуль отвечает только за тайминг анимации и выбор цели.
[Serializable]
[AddTypeMenu("Способности/Каст способности")]
public class AbilityAttackModule : EnemyAttackModule
{
    public AbilityDefinition ability;

    [Tooltip("Целиться в игрока. Выключено = целимся в собственную позицию (для аур и самобафов)")]
    public bool aimAtTarget = true;

    public override bool CanUse(EnemyController controller)
    {
        return ability != null && controller.Attack.IsAbilityReady(ability);
    }

    public override void Execute(EnemyController controller)
    {
        IAbilityTarget target = ResolveTarget(controller);
        Vector2 aim = aimAtTarget && target != null && target.Transform != null
            ? (Vector2)target.Transform.position
            : (Vector2)controller.transform.position;

        controller.Attack.TryCastAbility(ability, target, aim);
    }

    // Миграция старых конфигов, где способности лежали в EnemyConfig.abilities отдельным списком.
    internal static AbilityAttackModule FromLegacy(AbilityDefinition definition, EnemyConfig config) => new()
    {
        label = $"{definition.name} (из устаревшего списка Abilities)",
        animatorParameter = "Hit",
        windup = config.attackDuration,
        ability = definition
    };
}
