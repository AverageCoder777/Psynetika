using UnityEngine;

// Единая точка нанесения урона по произвольному объекту сцены.
// Заменяет дублирующиеся пары GetComponent<Enemy>/GetComponent<DamageDummy> у пуль, снарядов и ударов.
public static class DamageHelper
{
    public static bool TryDamage(Component hit, float amount, DamageType type,
        IAbilityCaster attacker = null, AbilityDefinition sourceAbility = null)
    {
        return TryDamage(hit, amount, type, out _, attacker, sourceAbility);
    }

    public static bool TryDamage(Component hit, float amount, DamageType type,
        out IAbilityTarget target, IAbilityCaster attacker = null, AbilityDefinition sourceAbility = null)
    {
        target = null;
        if (hit == null) return false;
        if (!hit.TryGetComponent(out target) || !target.IsAlive)
        {
            target = null;
            return false;
        }

        target.ReceiveDamage(new DamageEvent
        {
            Attacker = attacker,
            Target = target,
            Amount = amount,
            Type = type,
            SourceAbility = sourceAbility
        });
        return true;
    }
}
