using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class ApplyStatBoostNode : AbilityNode
{
    public StatId stat = StatId.Damage;
    public float amount = 0.1f;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Owner is not IAbilityStatOwner ownerStats)
        {
            Debug.LogWarning("[ApplyStatBoostNode] Owner does not implement IAbilityStatOwner.");
            return UniTask.FromResult(NodeResult.Failure);
        }

        ownerStats.SetStat(stat, ownerStats.GetStat(stat) + amount);
        return UniTask.FromResult(NodeResult.Success);
    }
}
