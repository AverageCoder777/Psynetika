using UnityEngine;

public class CoinsScript : MonoBehaviour
{
    [SerializeField] CoinsUI coinsManager;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(("Player")))
        {
            coinsManager.coins++;
            Destroy(gameObject);
        }
    }
}
