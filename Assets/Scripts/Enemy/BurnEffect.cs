using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

public class BurnEffect : MonoBehaviour
{
    private Enemy enemy;
    //private Coroutine burnRoutine;

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

       /* if (burnRoutine != null)
        {
            StopCoroutine(burnRoutine);
        }
*/
        //burnRoutine = StartCoroutine(BurnCoroutine(safeDuration, safeTick, safeDamage));
        Burn(safeDuration, safeTick, safeDamage).Forget();
    }

    private async UniTaskVoid Burn(float duration, float tickInterval, int tickDamage)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            //yield return new WaitForSeconds(tickInterval);
            await UniTask.Delay((int)duration);
            elapsed += tickInterval;

            if (enemy == null)
            {
                return;
            }

            enemy.TakeDamage(tickDamage);
        }
    }
}
