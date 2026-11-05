using Cysharp.Threading.Tasks;
using UnityEngine;

[System.Serializable]
[AddTypeMenu("Урон/Урон цели")]
public class DamageNode : AbilityNode
{
    [Min(0f)] public float amount = 10f;
    [Tooltip("Если задан и есть в Blackboard — урон берётся оттуда (например, из «Расчёт урона от атаки»), а не из amount")]
    [BlackboardKeyInput] public string inputDamageKey = "";
    public DamageType type = DamageType.Physical;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.ResolvedTarget == null || !ctx.ResolvedTarget.IsAlive)
        {
            return UniTask.FromResult(NodeResult.Failure);
        }

        float damage = amount;
        if (!string.IsNullOrWhiteSpace(inputDamageKey) && ctx.Blackboard.TryGet(inputDamageKey, out float fromBlackboard))
        {
            damage = fromBlackboard;
        }

        DamageEvent damageEvent = new DamageEvent
        {
            Attacker = ctx.Owner,
            Target = ctx.ResolvedTarget,
            Amount = damage,
            Type = type,
            SourceAbility = ctx.Definition
        };

        ctx.ResolvedTarget.ReceiveDamage(damageEvent);
        return UniTask.FromResult(NodeResult.Success);
    }
}
