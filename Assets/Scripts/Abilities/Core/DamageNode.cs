using Cysharp.Threading.Tasks;
using UnityEngine;

[System.Serializable]
[AddTypeMenu("Урон/Урон цели")]
public class DamageNode : AbilityNode
{
    [Min(0f)] public float amount = 10f;
    public DamageType type = DamageType.Physical;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.ResolvedTarget == null || !ctx.ResolvedTarget.IsAlive)
        {
            return UniTask.FromResult(NodeResult.Failure);
        }

        DamageEvent damageEvent = new DamageEvent
        {
            Attacker = ctx.Owner,
            Target = ctx.ResolvedTarget,
            Amount = amount,
            Type = type,
            SourceAbility = ctx.Definition
        };

        ctx.ResolvedTarget.ReceiveDamage(damageEvent);
        return UniTask.FromResult(NodeResult.Success);
    }
}
