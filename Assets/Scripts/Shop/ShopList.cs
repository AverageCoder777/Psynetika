using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Psynetika/Shop/ShopList")]
public class ShopList : ScriptableObject
{
    [System.Serializable]
    public struct ShopItemEntry
    {
        public SpellData spell;
        public int amount;
    }

    [SerializeField] private List<ShopItemEntry> shoppingItems = new List<ShopItemEntry>();

    public IReadOnlyList<ShopItemEntry> ShoppingItems => shoppingItems;

    public Dictionary<SpellData, int> ToDictionary()
    {
        Dictionary<SpellData, int> result = new Dictionary<SpellData, int>();

        foreach (ShopItemEntry entry in shoppingItems)
        {
            if (entry.spell == null)
            {
                continue;
            }

            result[entry.spell] = entry.amount;
        }

        return result;
    }
}
