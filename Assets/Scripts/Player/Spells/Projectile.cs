using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;

    [Header("Damage")]
    [SerializeField] private int damage = 10;

    private LayerMask enemyMask;
    private Rigidbody2D rb;

    public void Launch(float direction)
    {
        enemyMask = LayerMask.GetMask("Enemy");
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = new Vector2(direction * speed, 0f);
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if ((enemyMask.value & (1 << other.gameObject.layer)) > 0)
        {
            if (other.gameObject.TryGetComponent(out Enemy enemy))
            {
                HandleEnemyHit(enemy);
            }
            else if (other.gameObject.TryGetComponent(out DamageDummy dummy))
            {
                HandleDummyHit(dummy);
            }
        }

        Destroy(gameObject);
    }

    private void HandleEnemyHit(Enemy enemy)
    {
        enemy.TakeDamage(damage);
        Effect(enemy);
    }

    private void HandleDummyHit(DamageDummy dummy)
    {
        dummy.TakeDamage(damage);
    }

    public virtual void Effect(Enemy enemy)
    {
        
    }
}
