// Доступ способностей к здоровью кастера (дрейн HP под Berserk и т.п.)
// без привязки к конкретному компоненту игрока или врага.
public interface IAbilityHealth
{
    int GetMaxHP();

    // Снимает до amount HP, не опуская здоровье ниже minHp; возвращает фактически снятое.
    int TryDrainHP(int amount, int minHp);
}
