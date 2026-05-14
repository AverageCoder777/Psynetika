using UnityEngine;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(Enemy))]
public class BurnEffect : MonoBehaviour
{
    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    public void Apply(float duration, float tickInterval, int tickDamage)
    {
        if (enemy == null)
        {
            return;
        }

        float safeDuration = Mathf.Max(0.1f, duration);
        float safeTick = Mathf.Max(0.05f, tickInterval);
        int safeDamage = Mathf.Max(1, tickDamage);

        Burn(safeDuration, safeTick, safeDamage).Forget();
    }

    private async UniTaskVoid Burn(float duration, float tickInterval, int tickDamage)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            int delayMs = Mathf.Max(1, Mathf.RoundToInt(tickInterval * 1000f));
            await UniTask.Delay(delayMs);
            elapsed += tickInterval;

            if (enemy == null)
            {
                return;
            }

            enemy.TakeDamage(tickDamage);
        }
    }
}
