using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// Запускает всех детей одновременно и ждёт завершения всех.
// Cancelled любого ребёнка отменяет весь узел; иначе Failure, если хоть один провалился.
[Serializable]
[AddTypeMenu("Логика/Параллельно")]
public class ParallelNode : AbilityNode
{
    [SerializeReference, SubclassSelector]
    public List<AbilityNode> children = new List<AbilityNode>();

    public override async UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (children == null || children.Count == 0)
        {
            return NodeResult.Success;
        }

        List<UniTask<NodeResult>> tasks = new List<UniTask<NodeResult>>(children.Count);
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] != null)
            {
                tasks.Add(children[i].Execute(ctx));
            }
        }

        NodeResult[] results = await UniTask.WhenAll(tasks);

        NodeResult aggregate = NodeResult.Success;
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i] == NodeResult.Cancelled)
            {
                return NodeResult.Cancelled;
            }
            if (results[i] == NodeResult.Failure)
            {
                aggregate = NodeResult.Failure;
            }
        }

        return aggregate;
    }
}
