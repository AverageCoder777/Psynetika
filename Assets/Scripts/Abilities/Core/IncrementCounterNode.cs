using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
[AddTypeMenu("Логика/Счётчик (стаки)")]
public class IncrementCounterNode : AbilityNode
{
    [BlackboardKeyOutput] public string key = "counter";
    [Min(1)] public int max = 5;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Blackboard == null)
        {
            return UniTask.FromResult(NodeResult.Failure);
        }

        int current = ctx.Blackboard.Get(key, 0);
        if (current >= Mathf.Max(1, max))
        {
            return UniTask.FromResult(NodeResult.Failure);
        }

        ctx.Blackboard.Set(key, current + 1);
        return UniTask.FromResult(NodeResult.Success);
    }
}
