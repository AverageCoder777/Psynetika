using UnityEngine;

public class CoinsScript : MonoBehaviour
{
    public int amountCoins = 1;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(("Player")))
        {
            var inventory = other.GetComponent<Inventory>();
            if (inventory != null)
            {
                inventory.AddCoins(amountCoins);
            }
            Destroy(gameObject);
        }
    }
}
