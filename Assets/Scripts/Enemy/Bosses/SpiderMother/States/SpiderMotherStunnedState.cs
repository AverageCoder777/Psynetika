using UnityEngine;

/*
Мама паук в стане (роль Stagger) после разбитого щита: стоит на земле, не атакует и уязвима.
Стан короткий (не дольше 5 секунд), потом паучиха уползает на стену и бой идёт по новому кругу.
*/
public class SpiderMotherStunnedState : EnemyStates
{
    private readonly SpiderMotherController boss;

    private float duration = 1f;
    private int flagHash;
    private float elapsed;

    public SpiderMotherStunnedState(SpiderMotherController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
        boss = controller;
    }

    public bool Begin(float seconds, string animatorParameter)
    {
        duration = Mathf.Max(0f, seconds);
        flagHash = string.IsNullOrEmpty(animatorParameter) ? 0 : Animator.StringToHash(animatorParameter);
        return ChangeState(EnemyStateId.Stagger);
    }

    public override void Enter()
    {
        elapsed = 0f;
        ClearLocomotionFlags();
        Movement.Stop();
        boss.SetPose(0);
        if (flagHash != 0) SetFlag(flagHash, true);
    }

    public override void LogicUpdate()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= duration)
        {
            boss.ClimbToWall();
        }
    }

    public override void PhysicsUpdate()
    {
        Movement.Execute(MoveIntent.Stop);
    }

    public override void Exit()
    {
        if (flagHash != 0) SetFlag(flagHash, false);
    }
}
