using UnityEngine;

public class DefaultProjectileFactory : IProjectileFactory
{
    public ProjectileRuntime Spawn(ProjectileDefinition definition, AbilityContext context, Vector2 spawnPosition, float direction)
    {
        if (definition == null || definition.prefab == null)
        {
            Debug.LogWarning("[DefaultProjectileFactory] ProjectileDefinition or prefab is missing.");
            return null;
        }

        GameObject instance = Object.Instantiate(definition.prefab, spawnPosition, Quaternion.identity);
        if (!instance.TryGetComponent(out ProjectileRuntime runtime))
        {
            runtime = instance.AddComponent<ProjectileRuntime>();
        }

        runtime.Initialize(definition, context, direction);
        return runtime;
    }
}
