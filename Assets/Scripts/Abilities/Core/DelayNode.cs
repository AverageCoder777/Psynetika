using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class DelayNode : AbilityNode
{
    [Min(0f)] public float seconds = 0.12f;

    public override async UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        int delayMs = Mathf.Max(0, Mathf.RoundToInt(seconds * 1000f));
        if (delayMs > 0)
        {
            await UniTask.Delay(delayMs, cancellationToken: ctx.Token);
        }
        return NodeResult.Success;
    }
}
