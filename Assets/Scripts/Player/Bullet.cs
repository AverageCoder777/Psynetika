using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 5f;
    public int damage = 10;

    protected LayerMask enemyMask;
    private Rigidbody2D rb;

    private void Start()
    {
        enemyMask = LayerMask.GetMask("Enemy");
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
        if ((enemyMask.value & (1 << other.gameObject.layer)) > 0)
        {
            if (other.gameObject.TryGetComponent<Enemy>(out var enemy))
            {
                HandleEnemyHit(enemy);
            }
            else
            {
                DamageDummy dummy = other.gameObject.GetComponent<DamageDummy>();
                if (dummy != null) dummy.TakeDamage(damage);
            }
        }
        Destroy(gameObject);
    }

    protected virtual void HandleEnemyHit(Enemy enemy)
    {
        enemy.TakeDamage(damage);
    }
}
