// Дальнобойный архетип: та же композиция компонентов, что у EnemyController,
// но состояние атаки стреляет пулями вместо ближнего удара.
public class RangedEnemyController : EnemyController
{
    protected override void CreateStates()
    {
        IdleState = new EnemyIdleState(this, StateMachine);
        FollowState = new EnemyFollowState(this, StateMachine);
        AttackState = new EnemyRangedAttackState(this, StateMachine);
    }
}
