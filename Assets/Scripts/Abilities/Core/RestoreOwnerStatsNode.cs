using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class RestoreOwnerStatsNode : AbilityNode
{
    public string attackSpeedKey = "berserk.baseAttackSpeed";
    public string damageKey = "berserk.baseDamage";

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Owner is not IAbilityStatOwner ownerStats)
        {
            Debug.LogWarning("[RestoreOwnerStatsNode] Owner does not implement IAbilityStatOwner.");
            return UniTask.FromResult(NodeResult.Failure);
        }

        if (ctx.Instance.Blackboard.TryGetValue(attackSpeedKey, out object baseSpeedObj) && baseSpeedObj is float baseSpeed)
        {
            ownerStats.AttackSpeedMultiplier = baseSpeed;
        }

        if (ctx.Instance.Blackboard.TryGetValue(damageKey, out object baseDamageObj) && baseDamageObj is float baseDamage)
        {
            ownerStats.DamageMultiplier = baseDamage;
        }

        return UniTask.FromResult(NodeResult.Success);
    }
}
