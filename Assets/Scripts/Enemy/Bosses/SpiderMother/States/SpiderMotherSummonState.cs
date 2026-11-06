using UnityEngine;

/*
Мама паук на земле (роль Summon): закрывается щитом-паутиной (SpiderWebShield) и рожает
микро-паучков. Под щитом неуязвима — бить надо щит.
  щит разбит           — стан (роль Stagger);
  не разбит за timeout — снимает щит и уползает на стену.
*/
public class SpiderMotherSummonState : EnemyStates
{
    private readonly SpiderMotherController boss;
    private readonly int shieldedHash;

    private float elapsed;
    private float spawnTimer;
    private int minionsLeft;
    private bool shieldBroken;

    public SpiderMotherSummonState(SpiderMotherController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
        boss = controller;
        string parameter = controller.Boss.groundPhase.shieldedAnimatorParameter;
        shieldedHash = string.IsNullOrEmpty(parameter) ? 0 : Animator.StringToHash(parameter);
    }

    private SpiderMotherGroundPhase Settings => boss.Boss.groundPhase;

    public override void Enter()
    {
        ClearLocomotionFlags();
        Movement.Stop();
        boss.SetPose(0);
        boss.FacePlayer();

        elapsed = 0f;
        spawnTimer = Settings.spawnInterval;
        minionsLeft = Settings.RollMinions();
        shieldBroken = false;

        if (boss.Shield != null)
        {
            boss.Shield.Broken += OnShieldBroken;
            boss.Shield.Raise(Settings.shieldHp);
            controller.Health.IsInvulnerable = true;
        }
        if (shieldedHash != 0) SetFlag(shieldedHash, true);
    }

    public override void LogicUpdate()
    {
        if (shieldBroken)
        {
            boss.Stun();
            return;
        }

        elapsed += Time.deltaTime;
        if (elapsed >= Settings.timeout)
        {
            boss.ClimbToWall();
            return;
        }

        if (minionsLeft <= 0) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        spawnTimer = Settings.spawnInterval;
        if (boss.CanSpawnMinion)
        {
            boss.SpawnMinion();
            minionsLeft--;
        }
        else
        {
            // Арена и так кишит паучками — больше не рожаем.
            minionsLeft = 0;
        }
    }

    public override void PhysicsUpdate()
    {
        Movement.Execute(MoveIntent.Stop);
    }

    public override void Exit()
    {
        if (boss.Shield != null)
        {
            boss.Shield.Broken -= OnShieldBroken;
            if (boss.Shield.IsUp) boss.Shield.Drop();
        }
        controller.Health.IsInvulnerable = false;
        if (shieldedHash != 0) SetFlag(shieldedHash, false);
    }

    // Переход — в LogicUpdate, а не посреди обработки урона по щиту.
    private void OnShieldBroken() => shieldBroken = true;
}
