using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class DamageFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField, Min(0.01f)] private float flashDuration = 0.15f;

    private EnemyHealth health;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashRoutine;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Died += ResetColor;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= ResetColor;
    }

    private void OnDamaged(DamageEvent ev)
    {
        if (spriteRenderer == null || !isActiveAndEnabled) return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        flashRoutine = StartCoroutine(Flash());
    }

    private IEnumerator Flash()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }

    private void ResetColor()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}
