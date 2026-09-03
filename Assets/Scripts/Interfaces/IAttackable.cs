public interface IAttackable{
    public T GetCurrentAttackStat<T>(AttackStatId id);
    public void SetCurrentAttackStat(AttackStatId id, float value);
}