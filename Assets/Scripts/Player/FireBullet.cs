using UnityEngine;

public class FireBullet : SpellBase
{
    [Header("Projectile")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private int damage = 10;

    [SerializeField] private bool applyBurn;
    [SerializeField] private float burnDuration = 2f;
    [SerializeField] private float burnTickInterval = 0.5f;
    [SerializeField] private int burnDamagePerTick = 2;

    private LayerMask enemyMask;
    private Rigidbody2D rb;

    private void Start()
    {
        enemyMask = LayerMask.GetMask("Enemy");
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    public override void Cast(Vector2 spawnPosition)
    {
        if (spellData is SpellSatanData satanData)
        {
            if (satanData.spellSpeed > 0f) speed = satanData.spellSpeed;
            damage = Mathf.RoundToInt(satanData.spellDamage);
        }

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = new Vector2(direction * speed, 0f);
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
                if (dummy != null) HandleDummyHit(dummy);
            }
        }

        Destroy(gameObject);
    }

    private void HandleEnemyHit(Enemy enemy)
    {
        enemy.TakeDamage(damage);

        if (!applyBurn)
        {
            return;
        }

        BurnEffect burnEffect = enemy.GetComponent<BurnEffect>();
        if (burnEffect == null)
        {
            burnEffect = enemy.gameObject.AddComponent<BurnEffect>();
        }

        burnEffect.Apply(burnDuration, burnTickInterval, burnDamagePerTick);
    }

    private void HandleDummyHit(DamageDummy dummy)
    {
        dummy.TakeDamage(damage);

        if (!applyBurn)
        {
            return;
        }

        DummyBurnEffect burnEffect = dummy.GetComponent<DummyBurnEffect>();
        if (burnEffect == null)
        {
            burnEffect = dummy.gameObject.AddComponent<DummyBurnEffect>();
        }

        burnEffect.Apply(burnDuration, burnTickInterval, burnDamagePerTick);
    }
}
