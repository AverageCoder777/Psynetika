using System;
using UnityEngine;
using TMPro;

public class CoinsUI : MonoBehaviour
{
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private Inventory inventory;

    private void Awake()
    {
        coinsText.text = $"Coins {inventory.coins}";
        inventory.InventoryChenged += ChangeCoins;
    }

    public void ChangeCoins()
    {
        coinsText.text = $"Coins {inventory.coins}";
    }
    
    
}
