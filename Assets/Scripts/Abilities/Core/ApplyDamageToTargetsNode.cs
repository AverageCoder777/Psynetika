using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
[AddTypeMenu("Урон/Урон выбранным целям")]
public class ApplyDamageToTargetsNode : AbilityNode
{
    [BlackboardKeyInput] public string inputTargetsKey = "area.targets";
    [BlackboardKeyInput] public string inputDamageKey = "area.damage";
    public DamageType type = DamageType.Physical;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx == null)
        {
            return UniTask.FromResult(NodeResult.Failure);
        }

        if (!ctx.Blackboard.TryGet(inputTargetsKey, out List<IAbilityTarget> targets) || targets.Count == 0)
        {
            return UniTask.FromResult(NodeResult.Success);
        }

        if (!ctx.Blackboard.TryGet(inputDamageKey, out float amount))
        {
            return UniTask.FromResult(NodeResult.Failure);
        }

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
