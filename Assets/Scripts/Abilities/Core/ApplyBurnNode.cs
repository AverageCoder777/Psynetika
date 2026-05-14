using Cysharp.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class ApplyBurnNode : AbilityNode
{
    [Min(0.1f)] public float duration = 2f;
    [Min(0.05f)] public float tickInterval = 0.5f;
    [Min(1)] public int tickDamage = 2;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.ResolvedTarget == null || !ctx.ResolvedTarget.IsAlive)
        {
            return UniTask.FromResult(NodeResult.Failure);
        }

        if (!ctx.ResolvedTarget.Transform.TryGetComponent(out BurnEffect burnEffect))
        {
            burnEffect = ctx.ResolvedTarget.Transform.gameObject.AddComponent<BurnEffect>();
        }

        burnEffect.Apply(duration, tickInterval, tickDamage);
        return UniTask.FromResult(NodeResult.Success);
    }
}
