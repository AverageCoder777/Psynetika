public interface IAbilityStatOwner
{
    int MaxHp { get; }
    int CurrentHp { get; }
    int TryDrainHP(int amount, int minHp);
    float AttackSpeedMultiplier { get; set; }
    float DamageMultiplier { get; set; }
    float GetStat(StatId stat);
    void SetStat(StatId stat, float value);
}
