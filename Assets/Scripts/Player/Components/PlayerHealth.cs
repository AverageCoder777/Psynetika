using UnityEngine;
using System;

[RequireComponent(typeof(PlayerController))]
public class PlayerHealth : MonoBehaviour, IDamagable
{
    [Header("Dog HP")]
    [SerializeField] private int dogMaxHP = 100;
    [SerializeField] private int dogHP = 100;

    [Header("Satan HP")]
    [SerializeField] private int satanMaxHP = 100;
    [SerializeField] private int satanHP = 100;
    private PlayerController player;
    public event Action<PlayerCharacterType, int, int> HpChanged;
    public event Action<PlayerCharacterType> Died;

    public void Awake()
    {
        RaiseAll();
        player = GetComponent<PlayerController>();
    }
    public int TryDrainHP(int amount, int minHp)
    {
        if (amount <= 0) return 0;

        var type = player.GetCurrentCharacterType();
        if (type == PlayerCharacterType.Satan)
        {
            int drainable = Mathf.Max(0, satanHP - minHp);
            int actualDrain = Mathf.Min(amount, drainable);
            if (actualDrain > 0)
            {
                satanHP -= actualDrain;
                RaiseHp(type);
                if (satanHP <= 0) Died?.Invoke(type);
            }
            return actualDrain;
        }
        else
        {
            int drainable = Mathf.Max(0, dogHP - minHp);
            int actualDrain = Mathf.Min(amount, drainable);
            if (actualDrain > 0)
            {
                dogHP -= actualDrain;
                RaiseHp(type);
                if (dogHP <= 0) Died?.Invoke(type);
            }
            return actualDrain;
        }
    }
    public int GetCurrentHPOfCharacter(PlayerCharacterType type)
        => type == PlayerCharacterType.Satan ? satanHP : dogHP;

    public int GetMaxHPOfCharacter(PlayerCharacterType type)
        => type == PlayerCharacterType.Satan ? satanMaxHP : dogMaxHP;
    public int GetCurrentHP()
    {
        return GetCurrentHPOfCharacter(player.GetCurrentCharacterType());
    }
    public int GetCurrentMaxHP()
    {
        return GetMaxHPOfCharacter(player.GetCurrentCharacterType());
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        var type = player.GetCurrentCharacterType();
        if (type == PlayerCharacterType.Satan)
        {
            satanHP = Mathf.Max(0, satanHP - amount);
            RaiseHp(type);
            if (satanHP <= 0) Died?.Invoke(type);
        }
        else
        {
            dogHP = Mathf.Max(0, dogHP - amount);
            RaiseHp(type);
            if (dogHP <= 0) Died?.Invoke(type);
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        var type = player.GetCurrentCharacterType();
        if (type == PlayerCharacterType.Satan)
        {
            satanHP = Mathf.Min(satanMaxHP, satanHP + amount);
            RaiseHp(type);
        }
        else
        {
            dogHP = Mathf.Min(dogMaxHP, dogHP + amount);
            RaiseHp(type);
        }
    }

    public void ResetHealth()
    {
        var type = player.GetCurrentCharacterType();
        if (type == PlayerCharacterType.Satan)
        {
            satanHP = satanMaxHP;
            RaiseHp(type);
        }
        else
        {
            dogHP = dogMaxHP;
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

