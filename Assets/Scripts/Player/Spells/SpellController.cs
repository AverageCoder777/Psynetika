using UnityEngine;

[RequireComponent(typeof(PlayerAttack))]
[RequireComponent(typeof(AbilityRunner))]
public class SpellController : MonoBehaviour
{
    [Header("Satan")]
    [SerializeField] private AbilityDefinition satanRegular;
    [SerializeField] private AbilityDefinition satanUltimate;

    [Header("Sobaka")]
    [SerializeField] private AbilityDefinition sobakaRegular;
    [SerializeField] private AbilityDefinition sobakaUltimate;

    private AbilityRunner abilityRunner;
    private PlayerAttack attackOwner;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        attackOwner = GetComponent<PlayerAttack>();
        abilityRunner = GetComponent<AbilityRunner>();
        if (abilityRunner == null)
        {
            abilityRunner = gameObject.AddComponent<AbilityRunner>();
        }

        if (abilityRunner.Caster == null && attackOwner != null)
        {
            abilityRunner.Initialize(attackOwner, new AbilityServices());
        }

        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.Died += OnOwnerDied;
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= OnOwnerDied;
        }
    }

    // Смерть прерывает активные касты: их cleanup откатит временные бафы (Berserk и т.п.).
    private void OnOwnerDied(PlayerCharacterType _)
    {
        if (abilityRunner != null)
        {
            abilityRunner.CancelAll();
        }
    }

    public bool TryGetReadyAbility(bool isSatan, SpellSlot slot, out AbilityDefinition ability)
    {
        ability = GetSlotData(isSatan, slot);
        if (ability == null || abilityRunner == null) return false;
        return abilityRunner.IsReady(ability);
    }

    public bool TryCast(bool isSatan, SpellSlot slot, IAbilityTarget target, Vector2 aimPosition)
    {
        if (!TryGetReadyAbility(isSatan, slot, out AbilityDefinition ability))
        {
            return false;
        }

        return abilityRunner.TryCast(ability, target, aimPosition);
    }

    public float GetCooldownProgress(bool isSatan, SpellSlot slot)
    {
        AbilityDefinition ability = GetSlotData(isSatan, slot);
        if (ability == null || abilityRunner == null) return 1f;
        return abilityRunner.GetCooldownProgress(ability);
    }

    public void SetAbility(AbilityDefinition ability, SpellOwner ownerType, SpellSlot slot)
    {
        if (ability == null) return;
        bool isSatan = ownerType == SpellOwner.Satan;
        bool isSobaka = ownerType == SpellOwner.Sobaka;
        if (!isSatan && !isSobaka)
        {
            Debug.LogWarning("Ability owner is not a player");
            return;
        }
        if (slot == SpellSlot.Regular)
        {
            if (isSatan) satanRegular = ability;
            else sobakaRegular = ability;
        }
        else
        {
            if (isSatan) satanUltimate = ability;
            else sobakaUltimate = ability;
        }
    }

    public AbilityDefinition GetAbilityData(bool isSatan, SpellSlot slot) => GetSlotData(isSatan, slot);

    private AbilityDefinition GetSlotData(bool isSatan, SpellSlot slot)
    {
        if (isSatan) return slot == SpellSlot.Regular ? satanRegular : satanUltimate;
        return slot == SpellSlot.Regular ? sobakaRegular : sobakaUltimate;
    }
}
