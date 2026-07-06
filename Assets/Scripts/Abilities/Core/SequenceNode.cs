using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
[AddTypeMenu("Логика/Последовательность")]
public class SequenceNode : AbilityNode
{
    [SerializeReference, SubclassSelector]
    public List<AbilityNode> children = new List<AbilityNode>();

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        return AbilityNodeList.Run(children, ctx);
    }
}
