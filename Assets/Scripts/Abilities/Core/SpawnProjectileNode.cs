using Cysharp.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class SpawnProjectileNode : AbilityNode
{
    public ProjectileDefinition projectile;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx == null || projectile == null || projectile.prefab == null)
        {
            Debug.LogWarning("[SpawnProjectileNode] ProjectileDefinition is not configured.");
            return UniTask.FromResult(NodeResult.Failure);
        }

        if (ctx.Services?.Projectiles == null)
        {
            Debug.LogWarning("[SpawnProjectileNode] ProjectileFactory is not available.");
            return UniTask.FromResult(NodeResult.Failure);
        }

        Vector2 spawnPosition = ctx.Owner.Center + new Vector2(ctx.Direction * projectile.spawnOffset.x, projectile.spawnOffset.y);
        ctx.Services.Projectiles.Spawn(projectile, ctx, spawnPosition, ctx.Direction);
        return UniTask.FromResult(NodeResult.Success);
    }
}
