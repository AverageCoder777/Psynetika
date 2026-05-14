using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class SnapshotOwnerStatsNode : AbilityNode
{
    public string attackSpeedKey = "berserk.baseAttackSpeed";
    public string damageKey = "berserk.baseDamage";
    public string stacksKey = "berserk.stacks";
    public string drainedKey = "berserk.drained";

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Owner is not IAbilityStatOwner ownerStats)
        {
            Debug.LogWarning("[SnapshotOwnerStatsNode] Owner does not implement IAbilityStatOwner.");
            return UniTask.FromResult(NodeResult.Failure);
        }

        ctx.Instance.Blackboard[attackSpeedKey] = ownerStats.AttackSpeedMultiplier;
        ctx.Instance.Blackboard[damageKey] = ownerStats.DamageMultiplier;
        ctx.Instance.Blackboard[stacksKey] = 0;
        ctx.Instance.Blackboard[drainedKey] = 0;
        return UniTask.FromResult(NodeResult.Success);
    }
}
