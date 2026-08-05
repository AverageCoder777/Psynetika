using UnityEngine;
using System;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerDynSettings))]
[RequireComponent(typeof(PlayerCharacterManager))]
public class PlayerHealth : MonoBehaviour, IDamagable
{
    private PlayerStaticSettings settings;
    private PlayerDynSettings status;
    private PlayerCharacterManager charManager;
    public event Action<PlayerCharacterType, int, int> HpChanged;
    public event Action<PlayerCharacterType> Died;

    public void Awake()
    {
        settings = Resources.Load<PlayerStaticSettings>("PlayerDefaultSettings");
        status = GetComponent<PlayerDynSettings>();
        status.satanHP = settings.health.satanMaxHP;
        status.dogHP = settings.health.dogMaxHP;
        charManager = GetComponent<PlayerCharacterManager>();
        RaiseAll();
    }
    
    public int TryDrainHP(int amount, int minHp)
    {
        if (amount <= 0) return 0;

        var type = charManager.GetCurrentCharacterType();
        if (type == PlayerCharacterType.Satan)
        {
            int drainable = Mathf.Max(0, status.satanHP - minHp);
            int actualDrain = Mathf.Min(amount, drainable);
            if (actualDrain > 0)
            {
                status.satanHP -= actualDrain;
                RaiseHp(type);
                if (status.satanHP <= 0) Died?.Invoke(type);
            }
            return actualDrain;
        }
        else
        {
            int drainable = Mathf.Max(0, status.dogHP - minHp);
            int actualDrain = Mathf.Min(amount, drainable);
            if (actualDrain > 0)
            {
                status.dogHP -= actualDrain;
                RaiseHp(type);
                if (status.dogHP <= 0) Died?.Invoke(type);
            }
            return actualDrain;
        }
    }
    public int GetCurrentHPOfCharacter(PlayerCharacterType type)
        => type == PlayerCharacterType.Satan ? status.satanHP : status.dogHP;

    public int GetMaxHPOfCharacter(PlayerCharacterType type)
        => type == PlayerCharacterType.Satan ? settings.health.satanMaxHP : settings.health.dogMaxHP;
    public int GetCurrentMaxHP()
        => charManager.GetCurrentCharacterType() == PlayerCharacterType.Satan ? 
        settings.health.satanMaxHP : settings.health.dogMaxHP;

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        var type = charManager.GetCurrentCharacterType();
        if (type == PlayerCharacterType.Satan)
        {
            status.satanHP = Mathf.Max(0, status.satanHP - amount);
            RaiseHp(type);
            if (status.satanHP <= 0) Died?.Invoke(type);
        }
        else
        {
            status.dogHP = Mathf.Max(0, status.dogHP - amount);
            RaiseHp(type);
            if (status.dogHP <= 0) Died?.Invoke(type);
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        var type = charManager.GetCurrentCharacterType();
        if (type == PlayerCharacterType.Satan)
        {
            status.satanHP = Mathf.Min(settings.health.satanMaxHP, status.satanHP + amount);
            RaiseHp(type);
        }
        else
        {
            status.dogHP = Mathf.Min(settings.health.dogMaxHP, status.dogHP + amount);
            RaiseHp(type);
        }
    }

    public void ResetHealth()
    {
        var type = charManager.GetCurrentCharacterType();
        if (type == PlayerCharacterType.Satan)
        {
            status.satanHP = settings.health.satanMaxHP;
            RaiseHp(type);
        }
        else
        {
            status.dogHP = settings.health.dogMaxHP;
            RaiseHp(type);
        }
    }

    private void RaiseAll()
    {
        RaiseHp(PlayerCharacterType.Satan);
        RaiseHp(PlayerCharacterType.Dog);
    }

    private void RaiseHp(PlayerCharacterType type)
    {
        HpChanged?.Invoke(type, GetCurrentHPOfCharacter(type), GetMaxHPOfCharacter(type));
    }
}