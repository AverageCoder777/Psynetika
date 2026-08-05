public interface IAbilityStatOwner
{
    float GetStatMult(StatMultId stat);
    void SetStatMult<T>(StatMultId stat, T value);
}
