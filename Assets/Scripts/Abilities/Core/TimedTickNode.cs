using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

[Serializable]
public class TimedTickNode : AbilityNode
{
    [Min(0.1f)] public float duration = 15f;
    [Min(0.1f)] public float tickInterval = 1.5f;

    [SerializeReference, SubclassSelector]
    public List<AbilityNode> onTick = new List<AbilityNode>();

    public override async UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        float safeDuration = Mathf.Max(0.1f, duration);
        float safeTickInterval = Mathf.Max(0.1f, tickInterval);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            int delayMs = Mathf.Max(1, Mathf.RoundToInt(safeTickInterval * 1000f));
            await UniTask.Delay(delayMs, cancellationToken: ctx.Token);
            elapsed += safeTickInterval;

            if (onTick == null)
            {
                continue;
            }

            for (int i = 0; i < onTick.Count; i++)
            {
                AbilityNode node = onTick[i];
                if (node == null)
                {
                    continue;
                }

                NodeResult result = await node.Execute(ctx);
                if (result == NodeResult.Cancelled)
                {
                    return result;
                }
            }
        }

        return NodeResult.Success;
    }
}
