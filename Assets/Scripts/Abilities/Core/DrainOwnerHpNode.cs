using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
[AddTypeMenu("Урон/Дрейн HP кастера")]
public class DrainOwnerHpNode : AbilityNode
{
    [Range(0f, 1f)] public float drainPercent = 0.1f;
    [Range(0f, 1f)] public float criticalHpPercent = 0.1f;
    [BlackboardKeyOutput] public string outputDrainedKey = "berserk.drained";

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Owner is not IAbilityHealth health)
        {
            Debug.LogWarning("[DrainOwnerHpNode] Owner does not implement IAbilityHealth.");
            return UniTask.FromResult(NodeResult.Failure);
        }

        int maxHp = health.GetMaxHP();
        int criticalHp = Mathf.Max(1, Mathf.RoundToInt(maxHp * Mathf.Clamp01(criticalHpPercent)));
        int drainAmount = Mathf.Max(1, Mathf.RoundToInt(maxHp * Mathf.Clamp01(drainPercent)));
        int actual = health.TryDrainHP(drainAmount, criticalHp);

        ctx.Blackboard.Set(outputDrainedKey, Mathf.Max(0, actual));
        return UniTask.FromResult(actual > 0 ? NodeResult.Success : NodeResult.Failure);
    }
}
