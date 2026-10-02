using UnityEngine;

// Лут врага: при смерти выбрасывает монеты по EnemyConfig.loot. Контроллер добавляет компонент сам.
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
        if (config == null || config.loot == null) return;

        // Из центра тела, а не из пивота у ног — иначе монеты рождаются в полу.
        Vector2 origin = TryGetComponent(out IAbilityCaster caster) ? caster.Center : (Vector2)transform.position;
        config.loot.Spawn(origin);
    }
}
