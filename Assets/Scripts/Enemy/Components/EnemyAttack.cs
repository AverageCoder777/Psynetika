using UnityEngine;

// Боевые статы врага и вход в систему способностей — зеркало PlayerAttack.
public class EnemyAttack : MonoBehaviour, IAbilityCaster, IAbilityDamageSource, IAbilityHealth
{
    private EnemyConfig config;
    private EnemyMovement movement;
    private EnemyHealth health;
    private AbilityRunner abilityRunner;

    private float damageMultiplier = 1f;
    private float attackSpeedMultiplier = 1f;
    private float moveSpeedMultiplier = 1f;

    public int MeleeDamage => Mathf.RoundToInt((config != null ? config.meleeDamage : 0) * damageMultiplier);
    // Скорость атаки: базовая из конфига (баланс) * рантайм-множитель (баффы способностей).
    public float AttackDuration =>
        (config != null ? config.attackDuration : 2f)
        / Mathf.Max(0.05f, (config != null ? config.attackSpeed : 1f) * attackSpeedMultiplier);

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        health = GetComponent<EnemyHealth>();
    }

    public void Initialize(EnemyConfig cfg)
    {
        config = cfg;
        if (config.abilities != null && config.abilities.Count > 0)
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

    private void CancelActiveCasts()
    {
        if (abilityRunner != null)
        {
            abilityRunner.CancelAll();
        }
    }

    public void DealMeleeDamage(IAbilityTarget target)
    {
        if (target == null || !target.IsAlive) return;

        target.ReceiveDamage(new DamageEvent
        {
            Attacker = this,
            Target = target,
            Amount = MeleeDamage,
            Type = DamageType.Physical
        });
    }

    // Простой выстрел: спавнит существующий класс Bullet, как Сатана-игрок в HittingState.
    // «Мощный» путь (снаряды с нодами, кулдауны) — через config.abilities + TryCastAbility.
    public void SpawnBullet()
    {
        if (config == null || config.bulletPrefab == null)
        {
            Debug.LogWarning($"[EnemyAttack] {name}: bulletPrefab не задан в EnemyConfig — стрелять нечем.");
            return;
        }

        float dir = movement != null ? movement.FacingDirection : 1f;
        Vector2 spawnPos = GetBodyCenter()
            + new Vector2(dir * config.bulletSpawnOffset.x, config.bulletSpawnOffset.y);

        GameObject bulletObj = Instantiate(config.bulletPrefab, spawnPos, Quaternion.identity);
        if (bulletObj.TryGetComponent(out Bullet bullet))
        {
            bullet.damage = Mathf.RoundToInt(config.bulletDamage * damageMultiplier);
            bullet.Attacker = this;
            bullet.SetDirection(dir);
        }
        else
        {
            Debug.LogWarning($"[EnemyAttack] {name}: у bulletPrefab нет компонента Bullet.");
        }
    }

    public bool TryCastAbility(AbilityDefinition def, IAbilityTarget target, Vector2 aimPosition)
    {
        return abilityRunner != null && abilityRunner.TryCast(def, target, aimPosition);
    }

    // IAbilityCaster
    Transform IAbilityCaster.Transform => transform;
    Vector2 IAbilityCaster.Center => GetBodyCenter();
    float IAbilityCaster.FacingDirection => movement != null ? movement.FacingDirection : 1f;
    Team IAbilityCaster.Team => Team.Enemy;
    MonoBehaviour IAbilityCaster.CoroutineHost => this;

    int IAbilityDamageSource.GetBaseHitDamage() => MeleeDamage;

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

    public float GetStat(StatMultId stat)
    {
        return stat switch
        {
            StatMultId.CurrentDamageMult => damageMultiplier,
            StatMultId.CurrentAttackSpeedMult => attackSpeedMultiplier,
            StatMultId.CurrentMoveSpeedMult => moveSpeedMultiplier,
            _ => 1f
        };
    }

    public void SetStat(StatMultId stat, float value)
    {
        switch (stat)
        {
            case StatMultId.CurrentDamageMult:
                damageMultiplier = value;
                break;
            case StatMultId.CurrentAttackSpeedMult:
                attackSpeedMultiplier = value;
                break;
            case StatMultId.CurrentMoveSpeedMult:
                moveSpeedMultiplier = value;
                break;
            default:
                Debug.LogWarning($"[EnemyAttack.SetStat] StatId {stat} is not supported.");
                break;
        }
    }

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
