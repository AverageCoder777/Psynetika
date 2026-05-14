using UnityEngine;
using System;

public class Inventory : MonoBehaviour
{
    [SerializeField] private SpellController _sc;
    public int coins = 0;
    public Action InventoryChenged;
    
    public AbilityDefinition _AbilitySatan;

    public void AddCoins(int amount)
    {
        coins += amount;
        InventoryChenged?.Invoke();
    }
    
    public  void RemoveCoins(int amount)
    {
        coins -= amount;
        InventoryChenged?.Invoke();
    }

    public void ChangeAbility(AbilityDefinition ability, SpellOwner owner, SpellSlot slot)
    {
        if (ability == null || _sc == null) return;
        _AbilitySatan = ability;
        _sc.SetAbility(ability, owner, slot);
    }
    
}
