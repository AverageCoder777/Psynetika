// Сырой урон в обход статус-пайплайна (ProcessIncomingDamage/MaybeApplyStatusFromDamage).
// Используется тиками статус-эффектов: тик Burn через ReceiveDamage заново накладывал бы Burn.
public interface IDirectDamageReceiver
{
    void ApplyDamage(DamageEvent ev);
}
