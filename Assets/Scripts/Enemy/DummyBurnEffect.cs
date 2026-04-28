using Cysharp.Threading.Tasks;
using UnityEngine;

public class DummyBurnEffect : MonoBehaviour
{
    private DamageDummy dummy;

    private void Awake()
    {
        dummy = GetComponent<DamageDummy>();
    }

    public void Apply(float duration, float tickInterval, int tickDamage)
    {
        if (dummy == null)
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
            await UniTask.Delay((int)(tickInterval * 1000f));
            elapsed += tickInterval;

            if (dummy == null)
            {
                return;
            }

            dummy.TakeDamage(tickDamage);
        }
    }
}
