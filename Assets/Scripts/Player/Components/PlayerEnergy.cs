using System;
using UnityEngine;

// Энергия ульты. У каждого героя своя шкала (значения лежат в PlayerDynSettings, как и HP).
// Копится от нанесённого урона, у героев с passiveRegenPerSecond — ещё и со временем;
// тратится только на ульту, и только целиком (SpellController пускает ульту при полной шкале).
[RequireComponent(typeof(PlayerDynSettings))]
[RequireComponent(typeof(PlayerCharacterManager))]
public class PlayerEnergy : MonoBehaviour
{
    private PlayerStaticSettings settings;
    private PlayerDynSettings status;
    private PlayerCharacterManager charManager;
    private PlayerHealth health;
    private SpellController spellController;

    // Герой, текущая энергия, максимум.
    public event Action<PlayerCharacterType, float, float> EnergyChanged;

    private void Awake()
    {
        settings = Resources.Load<PlayerStaticSettings>("PlayerDefaultSettings");
        status = GetComponent<PlayerDynSettings>();
        charManager = GetComponent<PlayerCharacterManager>();
        health = GetComponent<PlayerHealth>();
        spellController = GetComponent<SpellController>();

        SetEnergy(PlayerCharacterType.Satan, GetSettings(PlayerCharacterType.Satan).startEnergyNormalized * GetMax(PlayerCharacterType.Satan));
        SetEnergy(PlayerCharacterType.Dog, GetSettings(PlayerCharacterType.Dog).startEnergyNormalized * GetMax(PlayerCharacterType.Dog));

        if (health != null)
        {
            health.Died += OnHeroDied;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= OnHeroDied;
        }
    }

    private void Update()
    {
        Regenerate(PlayerCharacterType.Satan);
        Regenerate(PlayerCharacterType.Dog);
    }

    public float Get(PlayerCharacterType hero) => hero == PlayerCharacterType.Satan ? status.satanEnergy : status.dogEnergy;

    public float GetMax(PlayerCharacterType hero) => Mathf.Max(1f, GetSettings(hero).maxEnergy);

    public float GetNormalized(PlayerCharacterType hero) => Mathf.Clamp01(Get(hero) / GetMax(hero));

    public bool IsFull(PlayerCharacterType hero) => Get(hero) >= GetMax(hero);

    // Возвращает фактически добавленное (с учётом потолка).
    public float Add(PlayerCharacterType hero, float amount)
    {
        if (amount <= 0f) return 0f;

        float before = Get(hero);
        SetEnergy(hero, before + amount);
        return Get(hero) - before;
    }

    // Обнуляет шкалу под каст ульты; возвращает потраченное — для возврата, если каст не состоялся.
    public float SpendAll(PlayerCharacterType hero)
    {
        float spent = Get(hero);
        SetEnergy(hero, 0f);
        return spent;
    }

    // Урон, нанесённый игроком (приходит через PlayerAttack как IDamageDealtObserver).
    // Энергию получает герой — владелец способности; для обычных атак — активный герой.
    public void OnDamageDealt(DamageEvent ev, int dealt)
    {
        if (dealt <= 0) return;

        PlayerCharacterType hero;
        HeroEnergySettings heroSettings;
        float multiplier;

        if (ev.SourceAbility != null && spellController != null &&
            spellController.TryFindAbilityOwner(ev.SourceAbility, out hero, out SpellSlot slot))
        {
            heroSettings = GetSettings(hero);
            multiplier = slot == SpellSlot.Ultimate
                ? heroSettings.ultimateDamageMultiplier
                : heroSettings.regularAbilityDamageMultiplier;
        }
        else
        {
            hero = charManager.GetCurrentCharacterType();
            heroSettings = GetSettings(hero);
            multiplier = heroSettings.basicAttackDamageMultiplier;
        }

        Add(hero, dealt * heroSettings.energyPerDamage * multiplier);
    }

    // Энергия от ноды способности (GainEnergyNode).
    public void AddFromAbility(AbilityDefinition source, float amount)
    {
        PlayerCharacterType hero = charManager.GetCurrentCharacterType();
        if (source != null && spellController != null)
        {
            spellController.TryFindAbilityOwner(source, out hero, out _);
        }

        Add(hero, amount);
    }

    private void Regenerate(PlayerCharacterType hero)
    {
        HeroEnergySettings heroSettings = GetSettings(hero);
        if (heroSettings.passiveRegenPerSecond <= 0f || IsFull(hero)) return;
        if (!heroSettings.regenWhileInactive && charManager.GetCurrentCharacterType() != hero) return;
        if (health != null && health.GetCurrentHPOfCharacter(hero) <= 0) return;

        Add(hero, heroSettings.passiveRegenPerSecond * Time.deltaTime);
    }

    private void OnHeroDied(PlayerCharacterType hero)
    {
        if (GetSettings(hero).resetOnDeath)
        {
            SetEnergy(hero, 0f);
        }
    }

    private void SetEnergy(PlayerCharacterType hero, float value)
    {
        float max = GetMax(hero);
        float clamped = Mathf.Clamp(value, 0f, max);
        if (Mathf.Approximately(clamped, Get(hero))) return;

        if (hero == PlayerCharacterType.Satan) status.satanEnergy = clamped;
        else status.dogEnergy = clamped;

        EnergyChanged?.Invoke(hero, clamped, max);
    }

    private HeroEnergySettings GetSettings(PlayerCharacterType hero)
        => hero == PlayerCharacterType.Satan ? settings.energy.satan : settings.energy.dog;
}
