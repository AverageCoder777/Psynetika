using UnityEngine;
using System.Collections;

public class EnemyFluid : MonoBehaviour
{
    [SerializeField] private float damageTime = 1.5f;
    [SerializeField] private int damageAmount = 10;
    private Coroutine damageCoroutine;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (damageCoroutine == null && other.CompareTag("Player")
            && other.TryGetComponent(out PlayerHealth playerHealth))
        {
            damageCoroutine = StartCoroutine(DamageOverTime(playerHealth));
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (damageCoroutine != null && other.CompareTag("Player"))
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
    }

    private IEnumerator DamageOverTime(PlayerHealth playerHealth)
    {
        if (damageTime <= 0f)
            yield break;

        while (playerHealth != null)
        {
            // Attacker = null: урон от окружения, без атакующего.
            DamageHelper.TryDamage(playerHealth, damageAmount, DamageType.Physical);
            yield return new WaitForSeconds(damageTime);
        }
    }
}
