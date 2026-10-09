// Цель, которую способности могут лечить (зоны поддержки и т.п.).
// Отдельно от IAbilityTarget: лечиться умеют не все носители урона (манекены, разрушаемые объекты).
public interface IAbilityHealable
{
    void ReceiveHeal(float amount);
}
