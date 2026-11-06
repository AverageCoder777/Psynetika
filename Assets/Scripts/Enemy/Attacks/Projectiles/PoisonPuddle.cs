using System.Collections.Generic;
using UnityEngine;

/*
Ядовитая лужа: лежит на поверхности lifetime секунд и травит игрока, пока он в ней стоит.
Отравление кратковременное (PoisonSettings источника); пока оно идёт, лужа его не перезапускает —
иначе сбрасывался бы таймер тика. Под конец жизни лужа выцветает.

Пивот лужи — середина нижнего края: Spawn ставит её точкой на поверхность.
*/
public class PoisonPuddle : MonoBehaviour
{
    private static readonly List<Collider2D> HitBuffer = new(4);

    [Tooltip("Зона отравления: ширина и высота над поверхностью")]
    [SerializeField] private Vector2 size = new(2.4f, 0.6f);

    [Tooltip("За сколько секунд до исчезновения лужа начинает выцветать")]
    [SerializeField, Min(0f)] private float fadeTime = 0.8f;

    [Tooltip("Как часто проверять, стоит ли в луже игрок, сек")]
    [SerializeField, Min(0.02f)] private float checkInterval = 0.1f;

    private PoisonSettings poison;
    private float lifetime;
    private float age;
    private float nextCheck;
    private SpriteRenderer sprite;
    private Color baseColor = Color.white;

    public static PoisonPuddle Spawn(PoisonPuddle prefab, Vector2 surfacePoint, float lifetime, PoisonSettings poison)
    {
        if (prefab == null) return null;

        PoisonPuddle puddle = Instantiate(prefab, surfacePoint, Quaternion.identity);
        puddle.lifetime = Mathf.Max(0.1f, lifetime);
        puddle.poison = poison;
        return puddle;
    }

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) baseColor = sprite.color;
        // Без Spawn (лужу поставили в сцену руками) живёт по умолчанию и травит дефолтным ядом.
        lifetime = 6f;
        poison = new PoisonSettings();
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (sprite != null && fadeTime > 0f)
        {
            float alpha = Mathf.Clamp01((lifetime - age) / fadeTime);
            sprite.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha);
        }

        if (age >= nextCheck)
        {
            nextCheck = age + checkInterval;
            PoisonPlayerInside();
        }
    }

    private void PoisonPlayerInside()
    {
        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Player")
        };

        Vector2 center = (Vector2)transform.position + Vector2.up * (size.y * 0.5f);
        int count = Physics2D.OverlapBox(center, size, 0f, filter, HitBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = HitBuffer[i];
            if (hit == null) continue;

            StatusEffectHandler handler = hit.GetComponentInParent<StatusEffectHandler>();
            if (handler != null && handler.IsPoisoned) continue;

            StatusEffectHandler.TryApplyPoison(hit, poison);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 1f, 0.3f, 0.7f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * (size.y * 0.5f), size);
    }
#endif
}
