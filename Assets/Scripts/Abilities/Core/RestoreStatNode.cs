using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class RestoreStatNode : AbilityNode
{
    public StatId stat = StatId.AttackSpeed;
    public string key = "save.stat";

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Owner is not IAbilityStatOwner ownerStats)
        {
            Debug.LogWarning("[RestoreStatNode] Owner does not implement IAbilityStatOwner.");
            return UniTask.FromResult(NodeResult.Failure);
        }

        if (ctx.Instance.Blackboard.TryGetValue(key, out object savedObj) && savedObj is float saved)
        {
            ownerStats.SetStat(stat, saved);
        }

        return UniTask.FromResult(NodeResult.Success);
    }
}
