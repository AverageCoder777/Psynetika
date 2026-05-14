using System;
using Cysharp.Threading.Tasks;

[Serializable]
public abstract class AbilityNode
{
    public abstract UniTask<NodeResult> Execute(AbilityContext ctx);
}
