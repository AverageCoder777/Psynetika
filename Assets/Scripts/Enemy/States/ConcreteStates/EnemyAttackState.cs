using UnityEngine;

/*
Универсальное состояние атаки: само по себе ничего не наносит, а проигрывает тайминги
выбранного EnemyAttackModule — замах, момент удара (module.Execute), отход.
Поэтому ближний бой, выстрел и каст способности используют одно и то же состояние.
*/
public class EnemyAttackState : EnemyStates
{
    private EnemyAttackModule module;
    private int parameterHash;
    private float elapsed;
    private bool executed;

    public EnemyAttackState(EnemyController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    public override void Enter()
    {
        elapsed = 0f;
        executed = false;
        module = Attack.PickAttack();
        if (module == null)
        {
            return;
        }

        parameterHash = string.IsNullOrEmpty(module.animatorParameter)
            ? 0
            : Animator.StringToHash(module.animatorParameter);

        if (module.faceTargetOnStart)
        {
            // Повернуться к игроку, чтобы удар/пуля шли в него, а не в последнюю сторону ходьбы.
            Transform target = Sensor.PlayerTransform;
            if (target != null)
            {
                Movement.Face(target.position.x - controller.transform.position.x);
            }
        }

        if (parameterHash != 0)
        {
            PlayTrigger(parameterHash);
            SetFlag(parameterHash, true);
        }
    }

    public override void LogicUpdate()
    {
        if (module == null)
        {
            ChangeState(Sensor.PlayerInFollowRange ? EnemyStateId.Follow : RestStateId);
            return;
        }

        // Игрок вышел из зоны удара до момента удара — замах срывается (так было и раньше).
        if (!executed && !Sensor.PlayerInHitRange)
        {
            ChangeState(Sensor.PlayerInFollowRange ? EnemyStateId.Follow : RestStateId);
            return;
        }

        // Замедление (Glitch) и бафы скорости атаки растягивают/сжимают весь цикл.
        elapsed += Time.deltaTime * Attack.AttackSpeedMultiplier;

        if (!executed)
        {
            if (elapsed < module.windup) return;

            module.Execute(controller);
            Attack.NotifyAttackUsed(module);
            executed = true;
            if (parameterHash != 0) SetFlag(parameterHash, false);
            return;
        }

        if (elapsed >= module.windup + module.recovery)
        {
            ChangeState(NextStateId());
        }
    }

    public override void Exit()
    {
        if (parameterHash != 0) SetFlag(parameterHash, false);
        module = null;
        parameterHash = 0;
        elapsed = 0f;
        executed = false;
    }

    // Цепочка атак: пока игрок в зоне удара и есть готовая атака, входим в состояние заново.
    private EnemyStateId NextStateId()
    {
        if (Sensor.PlayerInHitRange && Attack.HasReadyAttack) return EnemyStateId.Attack;
        if (Sensor.PlayerInFollowRange || Sensor.PlayerInHitRange) return EnemyStateId.Follow;
        return RestStateId;
    }
}
