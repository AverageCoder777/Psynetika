using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
[AddTypeMenu("Урон/Расчёт урона от атаки")]
public class ComputeOwnerHitDamageNode : AbilityNode
{
    [BlackboardKeyOutput] public string outputDamageKey = "area.damage";
    [Min(0f)] public float damageMultiplier = 1.5f;
    [Tooltip("Считать от текущего урона атаки с бафами кастера, а не от базового")]
    public bool includeOwnerBuffs = false;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Owner is not IAbilityDamageSource damageSource)
        {
            Debug.LogWarning("[ComputeOwnerHitDamageNode] Owner does not implement IAbilityDamageSource.");
            return UniTask.FromResult(NodeResult.Failure);
        }

        int hitDamage = includeOwnerBuffs ? damageSource.GetCurrentHitDamage() : damageSource.GetBaseHitDamage();
        float damage = Mathf.Max(0f, hitDamage * Mathf.Max(0f, damageMultiplier));
        ctx.Blackboard.Set(outputDamageKey, damage);
        return UniTask.FromResult(NodeResult.Success);
    }
}
