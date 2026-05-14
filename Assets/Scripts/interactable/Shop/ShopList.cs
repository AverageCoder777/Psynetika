using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Psynetika/Shop/ShopList")]
public class ShopList : ScriptableObject
{
    [System.Serializable]
    public struct ShopItem
    {
        public AbilityDefinition ability;
        public SpellOwner owner;
        public SpellSlot slot;
        public int amount;
    }

    [SerializeField] private List<ShopItem> shoppingItems = new List<ShopItem>();

    public IReadOnlyList<ShopItem> ShoppingItems => shoppingItems;

    public Dictionary<AbilityDefinition, int> ToDictionary()
    {
        Dictionary<AbilityDefinition, int> result = new Dictionary<AbilityDefinition, int>();

        foreach (ShopItem entry in shoppingItems)
        {
            if (entry.ability == null)
            {
                continue;
            }

            result[entry.ability] = entry.amount;
        }

        return result;
    }
    
}
