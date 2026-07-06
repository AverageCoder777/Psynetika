using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAttack : MonoBehaviour, IAbilityCaster, IAbilityDamageSource, IAbilityStatOwner, IAbilityHealth
{
    [Header("Удары Собаки")]
    [Tooltip("Скорость удара задает время на один удар")]
    [SerializeField] private float dogHitTime = 1f;
    [SerializeField] private float dogHitDistance = 1f;
    [SerializeField] private int dogHitDamage = 10;

    [Header("Стрельба Сатаны")]
    public GameObject bulletPrefab;
    [Tooltip("Скорость удара задает время на один выстрел")]
    [SerializeField] private float satanHitTime = 2f;
    [SerializeField] private float satanHitDistance = 2f;
    [SerializeField] private int satanHitDamage = 22;

    [Header("Множители")]
    [SerializeField] private float attackSpeedMultiplier = 1f;
    [SerializeField] private float damageMultiplier = 1f;
    Transform IAbilityCaster.Transform => transform;
    Vector2 IAbilityCaster.Center => TryGetComponent(out BoxCollider2D box) ? (Vector2)box.bounds.center : (Vector2)transform.position;
    float IAbilityCaster.FacingDirection => (player.PlayerCharManager.ActiveSR != null && player.PlayerCharManager.ActiveSR.flipX) ? -1f : 1f;
    Team IAbilityCaster.Team => Team.Player;
    MonoBehaviour IAbilityCaster.CoroutineHost => this;

    private PlayerController player;
    private PlayerHealth health;

    public float AttackSpeedMultiplier
    {
        get => attackSpeedMultiplier;
        set => attackSpeedMultiplier = value;
    }

    public float DamageMultiplier
    {
        get => damageMultiplier;
        set => damageMultiplier = value;
    }

    public void SetStat(StatId stat, float value)
    {
        switch (stat)
        {
            case StatId.AttackSpeed:
                AttackSpeedMultiplier = value;
                break;
            case StatId.Damage:
                DamageMultiplier = value;
                break;
            case StatId.MoveSpeed:
                Debug.LogWarning($"[PlayerAttack.SetStat] StatId {stat} is not supported here.");
                break;
            default:
                Debug.LogWarning($"[PlayerAttack.SetStat] StatId {stat} is not supported.");
                break;
        }
    }

    public float GetStat(StatId stat)
    {
        return stat switch
        {
            StatId.AttackSpeed => AttackSpeedMultiplier,
            StatId.Damage => DamageMultiplier,
            StatId.MoveSpeed => 1f,
            _ => 1f
        };
    }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        health = GetComponent<PlayerHealth>();
    }

    public float GetHitTime()
    {
        float baseHitTime = player != null && player.GetCurrentCharacterType() == PlayerCharacterType.Satan
            ? satanHitTime
            : dogHitTime;

        float safeAttackSpeedMultiplier = Mathf.Max(0.05f, AttackSpeedMultiplier);
        return baseHitTime / safeAttackSpeedMultiplier;
    }

    public float GetHitDistance()
    {
        if (player != null && player.GetCurrentCharacterType() == PlayerCharacterType.Satan)
        {
            return satanHitDistance;
        }

        return dogHitDistance;
    }

    public int GetHitDamage()
    {
        int baseDamage = player != null && player.GetCurrentCharacterType() == PlayerCharacterType.Satan
            ? satanHitDamage
            : dogHitDamage;

        return Mathf.RoundToInt(baseDamage * DamageMultiplier);
    }

    int IAbilityDamageSource.GetBaseHitDamage() => GetHitDamage();

    int IAbilityHealth.GetMaxHP() => health != null ? health.GetCurrentMaxHP() : 0;
    int IAbilityHealth.TryDrainHP(int amount, int minHp) => health != null ? health.TryDrainHP(amount, minHp) : 0;
}

