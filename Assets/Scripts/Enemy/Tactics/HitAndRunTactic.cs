using System;
using UnityEngine;
using Random = UnityEngine.Random;

/*
Наскоки: пока атака на перезарядке, враг хаотично мечется вокруг игрока на расстоянии,
как только атака готова (и подошла очередь команды, см. EnemyConfig.squad) — бросается к игроку.
Сам удар запускает режим Engage, когда игрок оказался в зоне атаки; после удара враг снова мечется.

Очередь команды тактика держит на всё сближение и отпускает, если не дотянулась до игрока за
approachTimeout (обрыв, стена) — тогда очередь переходит к следующему в команде.
*/
[Serializable]
[AddTypeMenu("Наскоки")]
public class HitAndRunTactic : EnemyTactic
{
    [Header("Сближение")]
    [Tooltip("Доля от moveSpeed при броске к игроку")]
    [Range(0.1f, 3f)] public float approachSpeedScale = 1.5f;

    [Tooltip("Не добежал до игрока за это время — уступить очередь и снова метаться, сек")]
    [Min(0.1f)] public float approachTimeout = 2.5f;

    [Tooltip("Пауза перед новой попыткой после неудачного сближения, сек")]
    [Min(0f)] public float giveUpPause = 1f;

    [Header("Метания между атаками")]
    [Tooltip("Доля от moveSpeed при метаниях")]
    [Range(0.1f, 3f)] public float skitterSpeedScale = 1f;

    [Tooltip("Ближе этого к игроку точки метаний не выбираются (по X)")]
    [Min(0f)] public float minDistance = 1.5f;

    [Tooltip("Дальше этого от игрока точки метаний не выбираются (по X)")]
    [Min(0f)] public float maxDistance = 4f;

    [Tooltip("Новая точка не реже, чем раз в столько секунд: от min до max")]
    [Min(0.05f)] public float retargetMin = 0.3f;
    [Min(0.05f)] public float retargetMax = 0.9f;

    [Tooltip("Шанс выбрать точку на своей стороне от игрока, а не перебегать на другую")]
    [Range(0f, 1f)] public float sameSideChance = 0.7f;

    [Tooltip("Разброс точек по Y — для летающих тел; наземному телу Y не важен")]
    [Min(0f)] public float verticalSpread = 0f;

    public override EnemyTacticRuntime CreateRuntime(EnemyController owner) => new Runtime(this, owner);

    private class Runtime : EnemyTacticRuntime
    {
        private readonly HitAndRunTactic settings;
        private Vector2 point;
        private bool hasPoint;
        private float retargetLeft;
        private bool approaching;
        private float approachTime;
        private float pauseLeft;

        public Runtime(HitAndRunTactic settings, EnemyController owner) : base(owner)
        {
            this.settings = settings;
        }

        public override void Enter()
        {
            hasPoint = false;
            approaching = false;
            approachTime = 0f;
            pauseLeft = 0f;
        }

        public override MoveIntent Tick(float deltaTime)
        {
            Transform target = Sensor.PlayerTransform;
            if (target == null)
            {
                StopApproach();
                return MoveIntent.Stop;
            }

            Vector2 player = target.position;
            if (pauseLeft > 0f)
            {
                pauseLeft -= deltaTime;
            }
            else if (Attack.HasAttackOffCooldown && Attack.TryTakeTurn())
            {
                approaching = true;
                approachTime += deltaTime;
                if (approachTime < settings.approachTimeout)
                {
                    return MoveIntent.To(player, settings.approachSpeedScale);
                }

                // Не дотянулся: пусть атакует следующий в команде, а мы побегаем.
                StopApproach();
                pauseLeft = settings.giveUpPause;
            }
            else
            {
                // Очередь перехватили (истёк maxTurnTime) или атака ушла на кулдаун.
                StopApproach();
            }

            return Skitter(player, deltaTime);
        }

        public override void Exit()
        {
            // При переходе в удар очередь тут же заберёт состояние атаки.
            StopApproach();
        }

        private MoveIntent Skitter(Vector2 player, float deltaTime)
        {
            retargetLeft -= deltaTime;
            if (!hasPoint || retargetLeft <= 0f || Movement.LastMoveBlocked || Movement.IsAt(point))
            {
                PickPoint(player);
            }
            return MoveIntent.To(point, settings.skitterSpeedScale);
        }

        private void PickPoint(Vector2 player)
        {
            float ownSide = Position.x >= player.x ? 1f : -1f;
            float side = Random.value < settings.sameSideChance ? ownSide : -ownSide;
            float min = Mathf.Min(settings.minDistance, settings.maxDistance);
            float max = Mathf.Max(settings.minDistance, settings.maxDistance);

            point = player + new Vector2(
                side * Random.Range(min, max),
                Random.Range(-settings.verticalSpread, settings.verticalSpread));
            retargetLeft = Random.Range(
                Mathf.Min(settings.retargetMin, settings.retargetMax),
                Mathf.Max(settings.retargetMin, settings.retargetMax));
            hasPoint = true;
        }

        private void StopApproach()
        {
            if (!approaching) return;

            approaching = false;
            approachTime = 0f;
            hasPoint = false;
            Attack.ReleaseTurn();
        }
    }
}
