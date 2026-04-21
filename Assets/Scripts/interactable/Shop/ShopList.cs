using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Psynetika/Shop/ShopList")]
public class ShopList : ScriptableObject
{
    [System.Serializable]
    public struct ShopItem
    {
        public SpellData spell;
        public int amount;
    }

    [SerializeField] private List<ShopItem> shoppingItems = new List<ShopItem>();

    public IReadOnlyList<ShopItem> ShoppingItems => shoppingItems;

    public Dictionary<SpellData, int> ToDictionary()
    {
        Dictionary<SpellData, int> result = new Dictionary<SpellData, int>();

        foreach (ShopItem entry in shoppingItems)
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
