using UnityEngine;
using System;

public class Inventory : MonoBehaviour
{
    [SerializeField] private SpellController _sc;
    public int coins = 0;
    public Action InventoryChenged;
    
    public SpellData _SpellSatan;

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

    public void ChangSpell(SpellData spell)
    {
        _SpellSatan =  spell;
        _sc.SetSpell(spell);
        
    }
    
}
