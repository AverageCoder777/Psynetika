using System.Collections.Generic;
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
    private PlayerEnergy energy;

    // Энергия, списанная под ещё не завершённый каст ульты, — вернётся, если каст не состоится.
    private readonly Dictionary<AbilityDefinition, (PlayerCharacterType hero, float amount)> pendingUltimateSpends = new();

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
        abilityRunner.CastFinished += OnCastFinished;

        energy = GetComponent<PlayerEnergy>();
        if (energy == null)
        {
            energy = gameObject.AddComponent<PlayerEnergy>();
        }

        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.Died += OnOwnerDied;
        }
    }

    private void OnDestroy()
    {
        if (abilityRunner != null)
        {
            abilityRunner.CastFinished -= OnCastFinished;
        }
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
        // Ульта требует полную шкалу энергии своего героя.
        if (slot == SpellSlot.Ultimate && !IsUltimateCharged(isSatan)) return false;
        return abilityRunner.IsReady(ability);
    }

    public bool TryCast(bool isSatan, SpellSlot slot, IAbilityTarget target, Vector2 aimPosition)
    {
        if (!TryGetReadyAbility(isSatan, slot, out AbilityDefinition ability))
        {
            return false;
        }

        // Списываем до TryCast: раннер может завершить (и провалить) каст синхронно,
        // и CastFinished должен уже видеть списанную сумму.
        if (slot == SpellSlot.Ultimate && energy != null)
        {
            PlayerCharacterType hero = ToHero(isSatan);
            pendingUltimateSpends[ability] = (hero, energy.SpendAll(hero));
        }

        bool casted = abilityRunner.TryCast(ability, target, aimPosition);
        if (!casted)
        {
            RefundUltimate(ability);
        }
        return casted;
    }

    public bool IsUltimateCharged(bool isSatan) => energy == null || energy.IsFull(ToHero(isSatan));

    public float GetUltimateEnergyProgress(bool isSatan) => energy != null ? energy.GetNormalized(ToHero(isSatan)) : 1f;

    // По способности находит героя и слот, в которых она стоит (для начисления энергии нужному герою).
    public bool TryFindAbilityOwner(AbilityDefinition ability, out PlayerCharacterType hero, out SpellSlot slot)
    {
        hero = PlayerCharacterType.Satan;
        slot = SpellSlot.Regular;
        if (ability == null) return false;

        if (ability == satanRegular) return true;
        slot = SpellSlot.Ultimate;
        if (ability == satanUltimate) return true;

        hero = PlayerCharacterType.Dog;
        slot = SpellSlot.Regular;
        if (ability == sobakaRegular) return true;
        slot = SpellSlot.Ultimate;
        return ability == sobakaUltimate;
    }

    private void OnCastFinished(AbilityDefinition ability, bool fizzled)
    {
        if (fizzled)
        {
            RefundUltimate(ability);
        }
        else
        {
            pendingUltimateSpends.Remove(ability);
        }
    }

    private void RefundUltimate(AbilityDefinition ability)
    {
        if (ability == null || !pendingUltimateSpends.Remove(ability, out var spend)) return;
        if (energy != null)
        {
            energy.Add(spend.hero, spend.amount);
        }
    }

    private static PlayerCharacterType ToHero(bool isSatan) => isSatan ? PlayerCharacterType.Satan : PlayerCharacterType.Dog;

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
