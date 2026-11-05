// Кастер, у которого способность может временно подменить обычные атаки (EmpoweredAttacksNode).
public interface IEmpoweredAttackOwner
{
    void BeginEmpoweredAttacks(EmpoweredAttackSession session);

    // Снимает режим, только если активен именно этот session (новый каст мог его уже заменить).
    void EndEmpoweredAttacks(EmpoweredAttackSession session);
}
