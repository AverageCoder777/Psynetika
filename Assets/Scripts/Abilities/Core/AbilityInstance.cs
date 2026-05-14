using System.Collections.Generic;

public class AbilityInstance
{
    public float CooldownEndsAt;
    public int Stacks;
    public bool IsExecuting;
    public readonly Dictionary<string, object> Blackboard = new Dictionary<string, object>();
}
