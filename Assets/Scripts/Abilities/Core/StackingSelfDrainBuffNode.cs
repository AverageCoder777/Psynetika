using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class StackingSelfDrainBuffNode : AbilityNode
{
    [Min(0.1f)] public float duration = 15f;
    [Min(0.1f)] public float tickInterval = 1.5f;
    [Range(0f, 1f)] public float drainPercentPerTick = 0.1f;
    [Range(0f, 1f)] public float criticalHpThreshold = 0.1f;
    [Range(0f, 1f)] public float boostPerStack = 0.1f;
    [Min(1)] public int maxStacks = 5;
    public bool buffAttackSpeed = true;
    public bool buffDamage = true;

    public override async UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Owner is not IAbilityStatOwner ownerStats)
        {
            Debug.LogWarning("[StackingSelfDrainBuffNode] Owner does not implement IAbilityStatOwner.");
            return NodeResult.Failure;
        }

        float safeDuration = Mathf.Max(0.1f, duration);
        float safeTickInterval = Mathf.Max(0.1f, tickInterval);
        float safeDrainPercent = Mathf.Clamp01(drainPercentPerTick);
        float safeCriticalThreshold = Mathf.Clamp01(criticalHpThreshold);
        float safeBoostPerStack = Mathf.Clamp01(boostPerStack);
        int safeMaxStacks = Mathf.Max(1, maxStacks);

        float baseAttackSpeed = ownerStats.AttackSpeedMultiplier;
        float baseDamage = ownerStats.DamageMultiplier;
        float elapsed = 0f;
        int stacks = 0;

        try
        {
            while (elapsed < safeDuration)
            {
                int delayMs = Mathf.Max(1, Mathf.RoundToInt(safeTickInterval * 1000f));
                await UniTask.Delay(delayMs, cancellationToken: ctx.Token);
                elapsed += safeTickInterval;

                if (stacks >= safeMaxStacks)
                {
                    continue;
                }

                int maxHp = Mathf.Max(1, ownerStats.MaxHp);
                int criticalHp = Mathf.Max(1, Mathf.RoundToInt(maxHp * safeCriticalThreshold));
                int drainAmount = Mathf.Max(1, Mathf.RoundToInt(maxHp * safeDrainPercent));
                int drained = ownerStats.TryDrainHP(drainAmount, criticalHp);

                if (drained <= 0)
                {
                    continue;
                }

                stacks++;
                if (buffAttackSpeed)
                {
                    ownerStats.AttackSpeedMultiplier += safeBoostPerStack;
                }
                if (buffDamage)
                {
                    ownerStats.DamageMultiplier += safeBoostPerStack;
                }
            }

            return NodeResult.Success;
        }
        finally
        {
            ownerStats.AttackSpeedMultiplier = baseAttackSpeed;
            ownerStats.DamageMultiplier = baseDamage;
        }
    }
}
