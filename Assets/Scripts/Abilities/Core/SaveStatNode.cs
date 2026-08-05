using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class SaveStatNode : AbilityNode
{
    public StatMultId stat = StatMultId.CurrentAttackSpeedMult;
    public string key = "save.stat";

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Owner is not IAbilityStatOwner ownerStats)
        {
            Debug.LogWarning("[SaveStatNode] Owner does not implement IAbilityStatOwner.");
            return UniTask.FromResult(NodeResult.Failure);
        }

        ctx.Instance.Blackboard[key] = ownerStats.GetStatMult(stat);
        return UniTask.FromResult(NodeResult.Success);
    }
}
