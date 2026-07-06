using System.Collections.Generic;
using UnityEngine;

// Спавнит снаряды через пул по префабу: вместо Destroy снаряд деактивируется
// и переиспользуется следующим Spawn с тем же префабом.
public class DefaultProjectileFactory : IProjectileFactory
{
    private readonly Dictionary<GameObject, Stack<ProjectileRuntime>> pools = new();

    public ProjectileRuntime Spawn(ProjectileDefinition definition, AbilityContext context, Vector2 spawnPosition, float direction)
    {
        if (definition == null || definition.prefab == null)
        {
            Debug.LogWarning("[DefaultProjectileFactory] ProjectileDefinition or prefab is missing.");
            return null;
        }

        GameObject prefab = definition.prefab;
        ProjectileRuntime runtime = TakeFromPool(prefab);
        if (runtime == null)
        {
            GameObject instance = Object.Instantiate(prefab, spawnPosition, Quaternion.identity);
            if (!instance.TryGetComponent(out runtime))
            {
                runtime = instance.AddComponent<ProjectileRuntime>();
            }
        }
        else
        {
            runtime.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
            runtime.gameObject.SetActive(true);
        }

        runtime.Initialize(definition, context, direction, r => Release(prefab, r));
        return runtime;
    }

    private ProjectileRuntime TakeFromPool(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out Stack<ProjectileRuntime> pool))
        {
            return null;
        }

        while (pool.Count > 0)
        {
            ProjectileRuntime runtime = pool.Pop();
            // Объекты пула могли быть уничтожены при выгрузке сцены.
            if (runtime != null)
            {
                return runtime;
            }
        }

        return null;
    }

    private void Release(GameObject prefab, ProjectileRuntime runtime)
    {
        if (runtime == null)
        {
            return;
        }

        runtime.gameObject.SetActive(false);
        if (!pools.TryGetValue(prefab, out Stack<ProjectileRuntime> pool))
        {
            pool = new Stack<ProjectileRuntime>();
            pools[prefab] = pool;
        }
        pool.Push(runtime);
    }
}
