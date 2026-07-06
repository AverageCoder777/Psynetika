using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// Ветвление: выполняет condition; Success ведёт в onSuccess, Failure — в onFailure.
// Сам Branch не проваливает внешнюю последовательность из-за результата condition.
[Serializable]
[AddTypeMenu("Логика/Ветвление")]
public class BranchNode : AbilityNode
{
    [SerializeReference, SubclassSelector]
    public AbilityNode condition;

    [SerializeReference, SubclassSelector]
    public List<AbilityNode> onSuccess = new List<AbilityNode>();

    [SerializeReference, SubclassSelector]
    public List<AbilityNode> onFailure = new List<AbilityNode>();

    public override async UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        NodeResult conditionResult = condition != null
            ? await condition.Execute(ctx)
            : NodeResult.Success;

        if (conditionResult == NodeResult.Cancelled)
        {
            return conditionResult;
        }

        List<AbilityNode> branch = conditionResult == NodeResult.Success ? onSuccess : onFailure;
        return await AbilityNodeList.Run(branch, ctx);
    }
}
