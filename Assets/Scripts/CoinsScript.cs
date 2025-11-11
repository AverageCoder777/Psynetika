using UnityEngine;

public class CoinsScript : MonoBehaviour
{
    [SerializeField] CoinsUI coinsManager;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == ("Player"))
        {
            coinsManager.coins++;
            PlayerPrefs.SetInt("Coins", coinsManager.coins);
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<BoxCollider2D>().enabled = false;
            print("Coins: " + coinsManager.coins);
        }
    }
}
