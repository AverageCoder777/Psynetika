using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
[AddTypeMenu("Энергия/Получить энергию ульты")]
public class GainEnergyNode : AbilityNode
{
    [Min(0f)] public float amount = 10f;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        // Кастер без шкалы (враг) — тихо пропускаем, чтобы нода не ломала общие способности.
        if (ctx?.Owner is IAbilityEnergyOwner owner && amount > 0f)
        {
            owner.AddEnergy(ctx.Definition, amount);
        }

        return UniTask.FromResult(NodeResult.Success);
    }
}
