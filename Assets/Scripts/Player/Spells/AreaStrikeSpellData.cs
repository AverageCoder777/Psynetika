using UnityEngine;

[CreateAssetMenu(menuName = "Psynetika/Area Strike Spell")]
public class AreaStrikeSpellData : SpellData
{
    [Header("Area Strike")]
    [Tooltip("Размер прямоугольной зоны удара (ширина × высота)")]
    public Vector2 hitBoxSize = new Vector2(2.5f, 1.5f);
    [Tooltip("Смещение центра зоны от центра игрока вдоль направления взгляда")]
    public float forwardOffset = 1.25f;
    [Tooltip("Множитель базового урона (1.1 = +10%)")]
    public float damageMultiplier = 1.1f;

    public override void Cast(SpellCastContext ctx)
    {
        if (ctx.Caster == null) return;

        Vector2 center = ctx.CasterCenter + Vector2.right * (ctx.Direction * forwardOffset);
        int dmg = Mathf.RoundToInt(ctx.Caster.GetHittingDamage() * damageMultiplier);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, hitBoxSize, 0f, LayerMask.GetMask("Enemy"));
        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            if (hit.TryGetComponent(out Enemy enemy))
            {
                enemy.TakeDamage(dmg);
                continue;
            }

            if (hit.TryGetComponent(out DamageDummy dummy))
            {
                dummy.TakeDamage(dmg);
            }
        }
    }
}
