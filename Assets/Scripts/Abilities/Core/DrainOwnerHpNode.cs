using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class DrainOwnerHpNode : AbilityNode
{
    [Range(0f, 1f)] public float drainPercent = 0.1f;
    [Range(0f, 1f)] public float criticalHpPercent = 0.1f;
    public string outputDrainedKey = "berserk.drained";

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Owner == null)
        {
            Debug.LogWarning("[DrainOwnerHpNode] AbilityContext.Owner is null.");
            return UniTask.FromResult(NodeResult.Failure);
        }
        
        var ownerTransform = ctx.Owner.Transform;
        if (ownerTransform == null)
        {
            Debug.LogWarning("[DrainOwnerHpNode] Owner.Transform is null.");
            return UniTask.FromResult(NodeResult.Failure);
        }

        var playerHealth = ownerTransform.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogWarning("[DrainOwnerHpNode] Owner does not have PlayerHealth component in parent chain.");
            return UniTask.FromResult(NodeResult.Failure);
        }

        int maxHp = playerHealth.GetCurrentMaxHP();
        int criticalHp = Mathf.Max(1, Mathf.RoundToInt(maxHp * Mathf.Clamp01(criticalHpPercent)));
        int drainAmount = Mathf.Max(1, Mathf.RoundToInt(maxHp * Mathf.Clamp01(drainPercent)));
        int actual = playerHealth.TryDrainHP(drainAmount, criticalHp);


        ctx.Instance.Blackboard[outputDrainedKey] = Mathf.Max(0, actual);
        return UniTask.FromResult(actual > 0 ? NodeResult.Success : NodeResult.Failure);
    }
}
