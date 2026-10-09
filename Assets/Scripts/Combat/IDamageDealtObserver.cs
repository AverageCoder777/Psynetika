// Атакующий, которому важно знать, сколько урона он реально нанёс (энергия ульты игрока).
// Вызывается целью после снятия HP через DamageHelper.NotifyDamageDealt — и для полного
// пайплайна (ReceiveDamage), и для сырого урона (ApplyDamage), если в событии указан Attacker.
public interface IDamageDealtObserver
{
    // dealt — фактически снятое HP (без оверкилла), всегда > 0.
    void OnDamageDealt(DamageEvent ev, int dealt);
}
