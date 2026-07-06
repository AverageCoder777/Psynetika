using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 5f;
    public int damage = 10;
    [SerializeField] private DamageType damageType = DamageType.Physical;
    [Tooltip("По каким слоям пуля наносит урон. Пусто = слой Enemy (пули игрока). Для пуль врага выставить слой Player.")]
    [SerializeField] private LayerMask targetLayers;

    protected LayerMask hitMask;
    private Rigidbody2D rb;

    public IAbilityCaster Attacker { get; set; }

    private void Start()
    {
        // Fallback на Enemy сохраняет поведение существующих префабов пуль игрока, где поле не задано.
        hitMask = targetLayers.value != 0 ? targetLayers : LayerMask.GetMask("Enemy");
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    public void SetDirection(float dir)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = new Vector2(dir * speed, 0f);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if ((hitMask.value & (1 << other.gameObject.layer)) > 0)
        {
            if (DamageHelper.TryDamage(other.collider, damage, damageType, out IAbilityTarget target, Attacker))
            {
                OnHit(target);
            }
        }
        Destroy(gameObject);
    }

    protected virtual void OnHit(IAbilityTarget target)
    {
    }
}
