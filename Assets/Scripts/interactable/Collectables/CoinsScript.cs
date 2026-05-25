using UnityEngine;

public class CoinsScript : MonoBehaviour
{
    public int amountCoins = 1;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<Inventory>(out var inventory))
            {
                inventory.AddCoins(amountCoins);
            }
            Destroy(gameObject);
        }
    }
}
