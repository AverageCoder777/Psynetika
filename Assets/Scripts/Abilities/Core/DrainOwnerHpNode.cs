using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class DrainOwnerHpNode : AbilityNode
{
    [Range(0f, 1f)] public float drainPercent = 0.1f;
    [Range(0f, 1f)] public float criticalHpThreshold = 0.1f;
    public string outputDrainedKey = "berserk.drained";

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Owner is not IAbilityStatOwner ownerStats)
        {
            Debug.LogWarning("[DrainOwnerHpNode] Owner does not implement IAbilityStatOwner.");
            return UniTask.FromResult(NodeResult.Failure);
        }

        int maxHp = Mathf.Max(1, ownerStats.MaxHp);
        int criticalHp = Mathf.Max(1, Mathf.RoundToInt(maxHp * Mathf.Clamp01(criticalHpThreshold)));
        int drainAmount = Mathf.Max(1, Mathf.RoundToInt(maxHp * Mathf.Clamp01(drainPercent)));
        int drained = ownerStats.TryDrainHP(drainAmount, criticalHp);
        ctx.Instance.Blackboard[outputDrainedKey] = drained;
        return UniTask.FromResult(drained > 0 ? NodeResult.Success : NodeResult.Failure);
    }
}
