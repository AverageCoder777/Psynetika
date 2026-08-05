using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerDynSettings))]
public class PlayerAttack : MonoBehaviour, IAbilityCaster, IAbilityDamageSource, IAbilityStatOwner, IAttackable
{
    private PlayerStaticSettings settings;
    private PlayerDynSettings status;
    public GameObject bulletPrefab;
    Transform IAbilityCaster.Transform => transform;
    Vector2 IAbilityCaster.Center => TryGetComponent(out BoxCollider2D box) ? (Vector2)box.bounds.center : (Vector2)transform.position;
    float IAbilityCaster.FacingDirection => (player.PlayerCharManager.ActiveSR != null && player.PlayerCharManager.ActiveSR.flipX) ? -1f : 1f;
    Team IAbilityCaster.Team => Team.Player;
    MonoBehaviour IAbilityCaster.CoroutineHost => this;
    public float AttackSpeedMultiplier { get => status.attackSpeedMultiplier; set => status.attackSpeedMultiplier = value; }
    public float DamageMultiplier { get => status.damageMultiplier; set => status.damageMultiplier = value; }
    private PlayerController player;
    public T GetCurrentAttackStat<T>(AttackStatId id)
    {
        switch (id)
        {
            case AttackStatId.CurrentDamage:
                return (T)(object)GetCurrentDamage();
            case AttackStatId.AttackSpeed:
                return (T)(object)GetAttackSpeed();
            case AttackStatId.AttackRange:
                return (T)(object)GetAttackRange();
            default:
                Debug.LogWarning($"[PlayerAttack.GetCurrentAttackStat] StatId {id} is not supported.");
                return default;
        }
    }
    public void SetCurrentAttackStat(AttackStatId id, float value)
    {
        switch (id)
        {
            case AttackStatId.AttackSpeed:
                if (player.PlayerCharManager.GetCurrentCharacterType() == PlayerCharacterType.Dog)
                {
                    status.dogAttackSpeedBoost = Mathf.RoundToInt(value);
                }
                else
                {
                    status.satanAttackSpeedBoost = Mathf.RoundToInt(value);
                }
                return;
            case AttackStatId.CurrentDamage:
                if (player.PlayerCharManager.GetCurrentCharacterType() == PlayerCharacterType.Dog)
                {
                    status.dogDamageBoost = Mathf.RoundToInt(value);
                }
                else
                {
                    status.satanDamageBoost = Mathf.RoundToInt(value);
                }
                return;
            case AttackStatId.AttackRange:
                if (player.PlayerCharManager.GetCurrentCharacterType() == PlayerCharacterType.Dog)
                {
                    status.dogAttackRangeBoost = Mathf.RoundToInt(value);
                }
                else
                {
                    status.satanAttackRangeBoost = Mathf.RoundToInt(value);
                }
                return;
            default:
                return;
        }
    }

    public void SetStatMult<T>(StatMultId stat, T value)
    {
        switch (stat)
        {
            case StatMultId.CurrentAttackSpeedMult:
                status.attackSpeedMultiplier = (float)(object)value;
                break;
            case StatMultId.CurrentDamageMult:
                status.damageMultiplier = (float)(object)value;
                break;
            case StatMultId.CurrentMoveSpeedMult:
                Debug.LogWarning($"[PlayerAttack.SetStat] StatId {stat} is not supported here.");
                break;
            default:
                Debug.LogWarning($"[PlayerAttack.SetStat] StatId {stat} is not supported.");
                break;
        }
    }

    public float GetStatMult (StatMultId stat)
    {
        return stat switch
        {
            StatMultId.CurrentAttackSpeedMult => status.attackSpeedMultiplier,
            StatMultId.CurrentDamageMult => status.damageMultiplier,
            StatMultId.CurrentMoveSpeedMult => status.speedMultiplier,
            _ => 1f
        };
    }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        settings = Resources.Load<PlayerStaticSettings>("PlayerDefaultSettings");
        status = GetComponent<PlayerDynSettings>();
    }
    private float GetAttackSpeed()
    {
        float baseSpeed = player.PlayerCharManager.GetCurrentCharacterType() == PlayerCharacterType.Dog ? settings.combat.dogBaseHitTime : settings.combat.satanBaseHitTime;
        return baseSpeed / status.attackSpeedMultiplier;
    }
    private int GetCurrentDamage()
    {
        int baseDamage = player.PlayerCharManager.GetCurrentCharacterType() == PlayerCharacterType.Dog ? settings.combat.dogBaseDamage : settings.combat.satanBaseDamage;
        int damageBoost = player.PlayerCharManager.GetCurrentCharacterType() == PlayerCharacterType.Dog ? status.dogDamageBoost : status.satanDamageBoost;
        return Mathf.RoundToInt((baseDamage + damageBoost) * status.damageMultiplier);
    }
    private float GetAttackRange()
    {
        float baseDistance = player.PlayerCharManager.GetCurrentCharacterType() == PlayerCharacterType.Dog ? settings.combat.dogBaseHitDistance : settings.combat.satanBaseHitDistance;
        return baseDistance;
    }

    int IAbilityDamageSource.GetBaseHitDamage() => Mathf.RoundToInt(GetStatMult(StatMultId.CurrentAttackSpeedMult));
}
