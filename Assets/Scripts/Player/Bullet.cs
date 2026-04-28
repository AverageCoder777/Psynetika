using UnityEngine;

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

    public virtual void ConfigureFromSpell(SpellSatanData spellData)
    {
        if (spellData == null) return;
        if (spellData.spellSpeed > 0f) speed = spellData.spellSpeed;
        damage = Mathf.RoundToInt(spellData.spellDamage);
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
            Enemy enemy = other.gameObject.GetComponent<Enemy>();
            if (enemy != null)
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
