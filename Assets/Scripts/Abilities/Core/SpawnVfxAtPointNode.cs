using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
[AddTypeMenu("Эффекты/VFX в точке")]
public class SpawnVfxAtPointNode : AbilityNode
{
    public GameObject vfxPrefab;

    [Tooltip("Смещение от точки каста (у снаряда — точка попадания/истечения)")]
    public Vector2 offset = Vector2.zero;

    [Min(0.1f)] public float lifetime = 2f;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (vfxPrefab == null)
        {
            Debug.LogWarning("[SpawnVfxAtPointNode] vfxPrefab is not assigned.");
            return UniTask.FromResult(NodeResult.Failure);
        }

        if (ctx == null)
        {
            return UniTask.FromResult(NodeResult.Failure);
        }

        // VFX живёт сам по себе: не привязан ни к кастеру, ни к снаряду, который сразу вернётся в пул.
        GameObject vfx = UnityEngine.Object.Instantiate(vfxPrefab, ctx.AimPosition + offset, Quaternion.identity);
        UnityEngine.Object.Destroy(vfx, lifetime);
        return UniTask.FromResult(NodeResult.Success);
    }
}
