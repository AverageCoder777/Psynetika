using UnityEngine;
using TMPro;

public class CoinsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private CoinsScript coinsScript;
    public int coins = 0;
    void Update()
    {
        if (coinsScript != null && coinsText != null)
        {
            coinsText.text = "Coins: " + coins; // Обновляем текст
        }
    }
}
