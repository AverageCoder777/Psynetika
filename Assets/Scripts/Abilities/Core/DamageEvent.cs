using System;
using System.Collections.Generic;

public struct DamageEvent
{
    public IAbilityCaster Attacker;
    public IAbilityTarget Target;
    public float Amount;
    public DamageType Type;
    public List<string> Tags;
    public AbilityDefinition SourceAbility;
}
