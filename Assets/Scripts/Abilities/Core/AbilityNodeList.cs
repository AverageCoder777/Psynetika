using System.Collections.Generic;
using Cysharp.Threading.Tasks;

// Единый способ выполнить список нод как последовательность:
// null-ноды пропускаются, первый не-Success результат останавливает список и возвращается наружу.
public static class AbilityNodeList
{
    public static async UniTask<NodeResult> Run(List<AbilityNode> nodes, AbilityContext ctx)
    {
        if (nodes == null || nodes.Count == 0)
        {
            return NodeResult.Success;
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            AbilityNode node = nodes[i];
            if (node == null)
            {
                continue;
            }

            NodeResult result = await node.Execute(ctx);
            if (result != NodeResult.Success)
            {
                return result;
            }
        }

        return NodeResult.Success;
    }
}
