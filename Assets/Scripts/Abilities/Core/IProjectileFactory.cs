using UnityEngine;

public interface IProjectileFactory
{
    ProjectileRuntime Spawn(ProjectileDefinition definition, AbilityContext context, Vector2 spawnPosition, float direction);
}
