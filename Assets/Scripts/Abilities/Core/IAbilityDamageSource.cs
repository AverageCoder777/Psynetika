public interface IAbilityDamageSource
{
    int GetBaseHitDamage();

    // Урон обычной атаки со всеми текущими бафами кастера.
    int GetCurrentHitDamage();
}
