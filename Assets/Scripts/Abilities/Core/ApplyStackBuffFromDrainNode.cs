using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class ApplyStackBuffFromDrainNode : AbilityNode
{
    public string stacksKey = "berserk.stacks";
    [Min(1)] public int maxStacks = 5;
    [Range(0f, 1f)] public float boostPerStack = 0.1f;
    public bool buffAttackSpeed = true;
    public bool buffDamage = true;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Owner is not IAbilityStatOwner ownerStats)
        {
            Debug.LogWarning("[ApplyStackBuffFromDrainNode] Owner does not implement IAbilityStatOwner.");
            return UniTask.FromResult(NodeResult.Failure);
        }

        int stacks = GetInt(ctx.Instance.Blackboard, stacksKey);
        if (stacks >= Mathf.Max(1, maxStacks))
        {
            return UniTask.FromResult(NodeResult.Success);
        }

        stacks++;
        ctx.Instance.Blackboard[stacksKey] = stacks;
        float boost = Mathf.Clamp01(boostPerStack);

        if (buffAttackSpeed)
        {
            ownerStats.AttackSpeedMultiplier += boost;
        }

        if (buffDamage)
        {
            ownerStats.DamageMultiplier += boost;
        }

        return UniTask.FromResult(NodeResult.Success);
    }

    private static int GetInt(System.Collections.Generic.Dictionary<string, object> blackboard, string key)
    {
        if (blackboard.TryGetValue(key, out object value))
        {
            if (value is int intValue) return intValue;
            if (value is float floatValue) return Mathf.RoundToInt(floatValue);
        }
        return 0;
    }
}
