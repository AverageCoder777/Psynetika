using UnityEngine;

public class FireBullet : Bullet
{
    [SerializeField] private bool applyBurn;
    [SerializeField] private float burnDuration = 2f;
    [SerializeField] private float burnTickInterval = 0.5f;
    [SerializeField] private int burnDamagePerTick = 2;

    protected override void HandleEnemyHit(Enemy enemy)
    {
        base.HandleEnemyHit(enemy);

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
}
