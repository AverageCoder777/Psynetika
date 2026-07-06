using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyLoot : MonoBehaviour
{
    private EnemyConfig config;
    private EnemyHealth health;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        health.Died += DropCoins;
    }

    private void OnDisable()
    {
        health.Died -= DropCoins;
    }

    public void Initialize(EnemyConfig cfg)
    {
        config = cfg;
    }

    private void DropCoins()
    {
        if (config == null || config.coinPrefab == null) return;

        for (int i = 0; i < config.coinsToDrop; i++)
        {
            GameObject coin = Instantiate(config.coinPrefab, transform.position, Quaternion.identity);
            coin.SetActive(true);
        }
    }
}
