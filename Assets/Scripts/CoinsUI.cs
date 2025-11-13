using UnityEngine;
using TMPro;

public class CoinsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private CoinsScript coinsScript;
    public int coins = 0;
    void Update()
    {
        
        coinsText.text = "Coins: " + coins;
        Debug.Log("Coins updated: " + coins); // Обновляем текст
        
    }
}
