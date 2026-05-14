using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class ApplyDamageToTargetsNode : AbilityNode
{
    public string inputTargetsKey = "area.targets";
    public string inputDamageKey = "area.damage";
    public DamageType type = DamageType.Physical;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx == null)
        {
            return UniTask.FromResult(NodeResult.Failure);
        }

        if (!ctx.Instance.Blackboard.TryGetValue(inputTargetsKey, out object targetsObj) ||
            targetsObj is not List<IAbilityTarget> targets ||
            targets.Count == 0)
        {
            return UniTask.FromResult(NodeResult.Success);
        }

        if (!ctx.Instance.Blackboard.TryGetValue(inputDamageKey, out object damageObj))
        {
            return UniTask.FromResult(NodeResult.Failure);
        }

        float amount = damageObj is float f ? f : (damageObj is int i ? i : 0f);
        if (amount <= 0f)
        {
            return UniTask.FromResult(NodeResult.Success);
        }

        for (int t = 0; t < targets.Count; t++)
        {
            IAbilityTarget target = targets[t];
            if (target == null || !target.IsAlive)
            {
                continue;
            }

            DamageEvent ev = new DamageEvent
            {
                Attacker = ctx.Owner,
                Target = target,
                Amount = amount,
                Type = type,
                SourceAbility = ctx.Definition
            };
            target.ReceiveDamage(ev);
        }

        return UniTask.FromResult(NodeResult.Success);
    }
}
