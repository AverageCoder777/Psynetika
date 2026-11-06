using UnityEngine;

/*
Мама паук на стене (роль Engage): сидит на точке и плюётся. Атаки — модули из config.attacks
(плевок паутиной, плевок ядом); выбор, кулдауны и замах — как у обычного врага (EnemyAttack.PickAttack),
только без проверки дистанции: со стены паучиха достаёт до любой точки арены.

Сделав attacksPerPerch атак, уходит с точки (SpiderMotherController.LeavePerch):
на другую точку или, если точки до спуска кончились, на землю.
*/
public class SpiderMotherWallState : EnemyStates
{
    private enum Phase
    {
        Waiting = 0,
        Windup = 1,
        Recovery = 2
    }

    private readonly SpiderMotherController boss;

    private Phase phase;
    private float timer;
    private float elapsed;
    private int attacksLeft;
    private EnemyAttackModule module;
    private int parameterHash;

    public SpiderMotherWallState(SpiderMotherController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
        boss = controller;
    }

    public override void Enter()
    {
        ClearLocomotionFlags();
        Movement.Stop();
        boss.SetPose(boss.Arena.SideOf(boss.Spot));

        phase = Phase.Waiting;
        timer = boss.Boss.wallPhase.settleTime;
        attacksLeft = boss.Boss.wallPhase.RollAttacks();
        module = null;
        parameterHash = 0;
    }

    public override void LogicUpdate()
    {
        switch (phase)
        {
            case Phase.Waiting:
                timer -= Time.deltaTime;
                if (timer > 0f) return;

                if (attacksLeft <= 0)
                {
                    boss.LeavePerch();
                    return;
                }
                // Нет готовой атаки (кулдауны, игрок не виден) — ждём на точке.
                StartWindup(Attack.PickAttack(ignoreRange: true));
                break;

            case Phase.Windup:
                elapsed += Time.deltaTime * Attack.AttackSpeedMultiplier;
                if (elapsed < module.windup) return;

                module.Execute(controller);
                Attack.NotifyAttackUsed(module);
                attacksLeft--;
                SetAttackFlag(false);
                phase = Phase.Recovery;
                elapsed = 0f;
                break;

            case Phase.Recovery:
                elapsed += Time.deltaTime * Attack.AttackSpeedMultiplier;
                if (elapsed < module.recovery) return;

                phase = Phase.Waiting;
                timer = boss.Boss.wallPhase.RollPause();
                module = null;
                break;
        }
    }

    public override void PhysicsUpdate()
    {
        Movement.Execute(MoveIntent.Stop);
    }

    public override void Exit()
    {
        SetAttackFlag(false);
        module = null;
        parameterHash = 0;
    }

    private void StartWindup(EnemyAttackModule picked)
    {
        if (picked == null) return;

        module = picked;
        parameterHash = string.IsNullOrEmpty(module.animatorParameter) ? 0 : Animator.StringToHash(module.animatorParameter);
        if (parameterHash != 0) PlayTrigger(parameterHash);
        SetAttackFlag(true);
        phase = Phase.Windup;
        elapsed = 0f;
    }

    private void SetAttackFlag(bool value)
    {
        if (parameterHash != 0) SetFlag(parameterHash, value);
    }
}
