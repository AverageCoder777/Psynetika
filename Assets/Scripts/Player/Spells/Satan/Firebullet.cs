using UnityEngine;

public class Firebullet : Projectile
{
    [Header("Burn")]
    [SerializeField] private bool applyBurn;
    [SerializeField] private float burnDuration = 2f;
    [SerializeField] private float burnTickInterval = 0.5f;
    [SerializeField] private int burnDamagePerTick = 2;

    public override void Effect(Enemy enemy)
    {
        if (!enemy.TryGetComponent(out BurnEffect burnEffect))
        {
            burnEffect = enemy.gameObject.AddComponent<BurnEffect>();
        }

        burnEffect.Apply(burnDuration, burnTickInterval, burnDamagePerTick);
    }
}
