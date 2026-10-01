using System;
using UnityEngine;
using Random = UnityEngine.Random;

/*
Настройки выпадения монет — общие для сундука и лута врага.
Монеты вылетают вверх веером ±spreadAngle от вертикали и pickupDelay секунд не подбираются
(см. Coin.Launch), поэтому успевают красиво раскидаться.
*/
[Serializable]
public class CoinDrop
{
    [Tooltip("Префаб с компонентом Coin и Rigidbody2D")]
    public GameObject coinPrefab;

    [Min(0)] public int minCount = 1;
    [Min(0)] public int maxCount = 3;

    [Header("Разлёт")]
    [Min(0f)] public float minForce = 4f;
    [Min(0f)] public float maxForce = 7f;

    [Tooltip("Половина угла веера от вертикали, градусы")]
    [Range(0f, 90f)] public float spreadAngle = 40f;

    [Tooltip("Сколько секунд выпавшую монету нельзя подобрать")]
    [Min(0f)] public float pickupDelay = 1f;

    public int RollCount() => Random.Range(minCount, Mathf.Max(minCount, maxCount) + 1);

    // Выбросить одну монету из точки origin.
    public Coin SpawnOne(Vector2 origin)
    {
        if (coinPrefab == null) return null;

        GameObject instance = UnityEngine.Object.Instantiate(coinPrefab, origin, Quaternion.identity);
        // Шаблоном может оказаться выключенный объект — монета в мире всегда активна.
        instance.SetActive(true);

        if (!instance.TryGetComponent(out Coin coin))
        {
            Debug.LogWarning($"[CoinDrop] на префабе '{coinPrefab.name}' нет компонента Coin.");
            return null;
        }

        float angle = Random.Range(-spreadAngle, spreadAngle);
        Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
        coin.Launch(direction * Random.Range(minForce, Mathf.Max(minForce, maxForce)), pickupDelay);
        return coin;
    }

    // Выбросить все монеты разом.
    public void Spawn(Vector2 origin)
    {
        int count = RollCount();
        for (int i = 0; i < count; i++)
        {
            SpawnOne(origin);
        }
    }
}
