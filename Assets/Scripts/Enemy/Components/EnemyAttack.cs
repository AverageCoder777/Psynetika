using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// Боевые статы врага, исполнение модулей атак и вход в систему способностей — зеркало PlayerAttack.
public class EnemyAttack : MonoBehaviour, IAbilityCaster, IAbilityDamageSource, IAbilityHealth, IAbilityStatOwner
{
    private EnemyConfig config;
    private EnemyController controller;
    private EnemyMovement movement;
    private EnemyHealth health;
    private EnemySensor sensor;
    private AbilityRunner abilityRunner;

    private readonly Dictionary<EnemyAttackModule, float> moduleReadyAt = new();
    private readonly List<EnemyAttackModule> candidates = new();
    private float globalReadyAt;

    private float damageMultiplier = 1f;
    private float attackSpeedMultiplier = 1f;
    private float moveSpeedMultiplier = 1f;

    // Скорость атаки: базовая из конфига (баланс) * рантайм-множитель (бафы способностей, замедление Glitch).
    public float AttackSpeedMultiplier =>
        Mathf.Max(0.05f, (config != null ? config.attackSpeed : 1f) * attackSpeedMultiplier);

    public bool HasReadyAttack => PickAttack() != null;

    public void Initialize(EnemyConfig cfg)
    {
        config = cfg;

        // Соседей ищем здесь, а не в Awake: контроллер мог доставить их после нашего Awake.
        controller = GetComponent<EnemyController>();
        movement = GetComponent<EnemyMovement>();
        health = GetComponent<EnemyHealth>();
        sensor = GetComponent<EnemySensor>();

        if (NeedsAbilityRunner())
        {
            if (!TryGetComponent(out abilityRunner))
            {
                abilityRunner = gameObject.AddComponent<AbilityRunner>();
            }
            abilityRunner.Initialize(this, new AbilityServices());

            if (health != null)
            {
                health.Died += CancelActiveCasts;
            }
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= CancelActiveCasts;
        }
    }

    private bool NeedsAbilityRunner()
    {
        foreach (EnemyAttackModule module in Attacks)
        {
            if (module is AbilityAttackModule)
            {
                return true;
            }
        }
        return false;
    }

    private void CancelActiveCasts()
    {
        if (abilityRunner != null)
        {
            abilityRunner.CancelAll();
        }
    }

    #region Выбор атаки
    private IReadOnlyList<EnemyAttackModule> Attacks =>
        config != null ? config.ResolveAttacks() : Array.Empty<EnemyAttackModule>();

    /*
    Подбирает атаку, готовую прямо сейчас: кулдауны (общий и персональный), дистанция до игрока
    и собственное условие модуля. Ничего не меняет — после фактического удара состояние обязано
    вызвать NotifyAttackUsed(), иначе кулдаун не запустится.
    */
    public EnemyAttackModule PickAttack()
    {
        IReadOnlyList<EnemyAttackModule> attacks = Attacks;
        if (attacks.Count == 0 || Time.time < globalReadyAt)
        {
            return null;
        }

        float distance = sensor != null ? sensor.HorizontalDistanceToPlayer : float.PositiveInfinity;
        bool pickFirst = config == null || config.attackSelection == EnemyConfig.AttackSelection.Priority;

        candidates.Clear();
        for (int i = 0; i < attacks.Count; i++)
        {
            EnemyAttackModule module = attacks[i];
            if (module == null) continue;
            if (moduleReadyAt.TryGetValue(module, out float readyAt) && Time.time < readyAt) continue;
            if (!IsInRange(module, distance)) continue;
            if (!module.CanUse(controller)) continue;

            candidates.Add(module);
            if (pickFirst) break;
        }

        if (candidates.Count == 0) return null;
        return pickFirst ? candidates[0] : candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private static bool IsInRange(EnemyAttackModule module, float distance)
    {
        if (module.maxRange > 0f && distance > module.maxRange) return false;
        if (module.minRange > 0f && distance < module.minRange) return false;
        return true;
    }

    // Запускает кулдауны после фактического применения атаки.
    public void NotifyAttackUsed(EnemyAttackModule module)
    {
        if (module == null) return;

        if (module.cooldown > 0f)
        {
            moduleReadyAt[module] = Time.time + module.cooldown;
        }
        if (config != null && config.attackCooldown > 0f)
        {
            globalReadyAt = Time.time + config.attackCooldown;
        }
    }
    #endregion

    #region Исполнение атак
    public void DealMeleeDamage(IAbilityTarget target, int damage, DamageType type = DamageType.Physical)
    {
        if (target == null || !target.IsAlive) return;

        int amount = Mathf.Max(0, Mathf.RoundToInt(damage * damageMultiplier));
        if (amount <= 0) return;

        target.ReceiveDamage(new DamageEvent
        {
            Attacker = this,
            Target = target,
            Amount = amount,
            Type = type
        });
    }

    // Выстрел простыми пулями. «Мощный» путь (снаряды с нодами, кулдауны) — через AbilityAttackModule.
    public void SpawnBullets(GameObject bulletPrefab, int damage, Vector2 spawnOffset, int shots = 1, float interval = 0f)
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning($"[EnemyAttack] {name}: в модуле выстрела не задан bulletPrefab — стрелять нечем.");
            return;
        }

        int safeShots = Mathf.Max(1, shots);
        if (safeShots == 1 || interval <= 0f)
        {
            for (int i = 0; i < safeShots; i++)
            {
                SpawnBullet(bulletPrefab, damage, spawnOffset);
            }
            return;
        }

        SpawnBurst(bulletPrefab, damage, spawnOffset, safeShots, interval).Forget();
    }

    private async UniTaskVoid SpawnBurst(GameObject bulletPrefab, int damage, Vector2 spawnOffset, int shots, float interval)
    {
        CancellationToken token = this.GetCancellationTokenOnDestroy();
        int delayMs = Mathf.Max(1, Mathf.RoundToInt(interval * 1000f));

        try
        {
            for (int i = 0; i < shots; i++)
            {
                if (i > 0)
                {
                    await UniTask.Delay(delayMs, cancellationToken: token);
                }
                if (health != null && !health.IsAlive) return;

                SpawnBullet(bulletPrefab, damage, spawnOffset);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void SpawnBullet(GameObject bulletPrefab, int damage, Vector2 spawnOffset)
    {
        float dir = movement != null ? movement.FacingDirection : 1f;
        Vector2 spawnPos = GetBodyCenter() + new Vector2(dir * spawnOffset.x, spawnOffset.y);

        GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        if (bulletObj.TryGetComponent(out Bullet bullet))
        {
            bullet.damage = Mathf.RoundToInt(damage * damageMultiplier);
            bullet.Attacker = this;
            bullet.SetDirection(dir);
        }
        else
        {
            Debug.LogWarning($"[EnemyAttack] {name}: у bulletPrefab нет компонента Bullet.");
        }
    }

    public bool IsAbilityReady(AbilityDefinition def) => abilityRunner != null && abilityRunner.IsReady(def);

    public bool TryCastAbility(AbilityDefinition def, IAbilityTarget target, Vector2 aimPosition)
    {
        if (abilityRunner == null)
        {
            Debug.LogWarning($"[EnemyAttack] {name}: AbilityRunner не создан — способность '{(def != null ? def.name : "null")}' не скастована.");
            return false;
        }
        return abilityRunner.TryCast(def, target, aimPosition);
    }
    #endregion

    #region Интерфейсы системы способностей
    Transform IAbilityCaster.Transform => transform;
    Vector2 IAbilityCaster.Center => GetBodyCenter();
    float IAbilityCaster.FacingDirection => movement != null ? movement.FacingDirection : 1f;
    Team IAbilityCaster.Team => Team.Enemy;
    MonoBehaviour IAbilityCaster.CoroutineHost => this;

    // Базовый урон «обычной атаки» для нод вроде ComputeOwnerHitDamage: первый ближний модуль врага.
    int IAbilityDamageSource.GetBaseHitDamage()
    {
        int baseDamage = config != null ? config.meleeDamage : 0;
        foreach (EnemyAttackModule module in Attacks)
        {
            if (module is MeleeAttackModule melee)
            {
                baseDamage = melee.damage;
                break;
            }
        }
        return Mathf.RoundToInt(baseDamage * damageMultiplier);
    }

    int IAbilityHealth.GetMaxHP() => health != null ? health.MaxHp : 0;

    int IAbilityHealth.TryDrainHP(int amount, int minHp)
    {
        if (health == null || !health.IsAlive || amount <= 0) return 0;

        int drainable = Mathf.Max(0, health.CurrentHp - Mathf.Max(0, minHp));
        int actual = Mathf.Min(amount, drainable);
        if (actual > 0)
        {
            // Сырой урон мимо статус-пайплайна: дрейн собственного HP не должен вешать статусы.
            health.ApplyDamage(new DamageEvent
            {
                Attacker = this,
                Target = health,
                Amount = actual,
                Type = DamageType.Physical
            });
        }
        return actual;
    }

    // IAbilityStatOwner: без него ApplyStatBoostNode и замедление от Glitch молча не работали на врагах.
    public float GetStatMult(StatMultId stat)
    {
        return stat switch
        {
            StatMultId.CurrentDamageMult => damageMultiplier,
            StatMultId.CurrentAttackSpeedMult => attackSpeedMultiplier,
            StatMultId.CurrentMoveSpeedMult => moveSpeedMultiplier,
            _ => 1f
        };
    }

    public void SetStatMult<T>(StatMultId stat, T value)
    {
        if (value is not float amount)
        {
            Debug.LogWarning($"[EnemyAttack.SetStatMult] Ожидался float, получен {typeof(T).Name}.");
            return;
        }

        switch (stat)
        {
            case StatMultId.CurrentDamageMult:
                damageMultiplier = amount;
                break;
            case StatMultId.CurrentAttackSpeedMult:
                attackSpeedMultiplier = amount;
                break;
            case StatMultId.CurrentMoveSpeedMult:
                moveSpeedMultiplier = amount;
                break;
            default:
                Debug.LogWarning($"[EnemyAttack.SetStatMult] StatId {stat} is not supported.");
                break;
        }
    }
    #endregion

    private Vector2 GetBodyCenter()
    {
        // На враге несколько коллайдеров (тело + 2 триггера) — центр берём по телу.
        foreach (Collider2D col in GetComponents<Collider2D>())
        {
            if (!col.isTrigger)
            {
                return col.bounds.center;
            }
        }
        return transform.position;
    }
}
