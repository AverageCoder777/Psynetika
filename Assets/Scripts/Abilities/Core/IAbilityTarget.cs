using UnityEngine;

public interface IAbilityTarget
{
    Transform Transform { get; }
    bool IsAlive { get; }
    Team Team { get; }
    void ReceiveDamage(DamageEvent ev);
}
