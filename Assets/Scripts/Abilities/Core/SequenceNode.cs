using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

[Serializable]
public class SequenceNode : AbilityNode
{
    [SerializeReference, SubclassSelector]
    public List<AbilityNode> children = new List<AbilityNode>();

    public override async UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (children == null || children.Count == 0)
        {
            return NodeResult.Success;
        }

        for (int i = 0; i < children.Count; i++)
        {
            AbilityNode child = children[i];
            if (child == null)
            {
                continue;
            }

            NodeResult result = await child.Execute(ctx);
            if (result != NodeResult.Success)
            {
                return result;
            }
        }

        return NodeResult.Success;
    }
}
