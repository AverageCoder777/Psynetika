using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class SelectTargetsInBoxNode : AbilityNode
{
    public string outputTargetsKey = "area.targets";
    public Vector2 boxSize = new Vector2(2.5f, 1.5f);
    public float forwardOffset = 1.25f;
    public string layerName = "Enemy";

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Owner == null)
        {
            return UniTask.FromResult(NodeResult.Failure);
        }

        Vector2 center = ctx.Owner.Center + new Vector2(ctx.Direction * forwardOffset, 0f);
        int mask = LayerMask.GetMask(string.IsNullOrWhiteSpace(layerName) ? "Enemy" : layerName);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, boxSize, 0f, mask);

        List<IAbilityTarget> targets = new List<IAbilityTarget>(hits.Length);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null) continue;
            if (hit.TryGetComponent(out IAbilityTarget target) && target.IsAlive)
            {
                targets.Add(target);
            }
        }

        ctx.Instance.Blackboard[outputTargetsKey] = targets;
        return UniTask.FromResult(NodeResult.Success);
    }
}
