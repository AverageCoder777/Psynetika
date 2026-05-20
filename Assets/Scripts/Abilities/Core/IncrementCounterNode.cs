using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class IncrementCounterNode : AbilityNode
{
    public string key = "counter";
    [Min(1)] public int max = 5;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Instance == null)
        {
            return UniTask.FromResult(NodeResult.Failure);
        }

        Dictionary<string, object> blackboard = ctx.Instance.Blackboard;
        int current = 0;
        if (blackboard.TryGetValue(key, out object value))
        {
            if (value is int intValue) current = intValue;
            else if (value is float floatValue) current = Mathf.RoundToInt(floatValue);
        }

        int safeMax = Mathf.Max(1, max);
        if (current >= safeMax)
        {
            return UniTask.FromResult(NodeResult.Failure);
        }

        blackboard[key] = current + 1;
        return UniTask.FromResult(NodeResult.Success);
    }
}
