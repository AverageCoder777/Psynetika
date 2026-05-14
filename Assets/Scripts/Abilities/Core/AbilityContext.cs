using System.Threading;
using UnityEngine;

public class AbilityContext
{
    public IAbilityCaster Owner;
    public IAbilityTarget ResolvedTarget;
    public Vector2 AimPosition;
    public float Direction;
    public AbilityDefinition Definition;
    public AbilityInstance Instance;
    public AbilityServices Services;
    public CancellationToken Token;
}
