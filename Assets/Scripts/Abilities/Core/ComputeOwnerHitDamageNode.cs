using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class ComputeOwnerHitDamageNode : AbilityNode
{
    public string outputDamageKey = "area.damage";
    [Min(0f)] public float damageMultiplier = 1.5f;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Owner is not IAbilityDamageSource damageSource)
        {
            Debug.LogWarning("[ComputeOwnerHitDamageNode] Owner does not implement IAbilityDamageSource.");
            return UniTask.FromResult(NodeResult.Failure);
        }

        float damage = Mathf.Max(0f, damageSource.GetBaseHitDamage() * Mathf.Max(0f, damageMultiplier));
        ctx.Instance.Blackboard[outputDamageKey] = damage;
        return UniTask.FromResult(NodeResult.Success);
    }
}
