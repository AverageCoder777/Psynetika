using System;

public struct DamageEvent
{
    public IAbilityCaster Attacker;
    public IAbilityTarget Target;
    public float Amount;
    public DamageType Type;
    public AbilityDefinition SourceAbility;
}
