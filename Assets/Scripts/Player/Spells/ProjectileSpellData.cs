using UnityEngine;

[CreateAssetMenu(menuName = "Psynetika/Projectile Spell")]
public class ProjectileSpellData : SpellData
{
    [Header("Projectile")]
    public GameObject projectilePrefab;

    [Tooltip("Смещение точки спавна относительно центра кастера (X вдоль направления взгляда)")]
    public Vector2 spawnOffset = new Vector2(0.65f, 0.22f);

    public override void Cast(SpellCastContext ctx)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[{name}] projectilePrefab не задан.");
            return;
        }

        Vector2 spawnPos = ctx.CasterCenter + new Vector2(ctx.Direction * spawnOffset.x, spawnOffset.y);
        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        if (!go.TryGetComponent(out Projectile projectile))
        {
            Debug.LogWarning($"[{name}] У projectilePrefab нет компонента Projectile.");
            Destroy(go);
            return;
        }

        projectile.Launch(ctx.Direction);
    }
}
