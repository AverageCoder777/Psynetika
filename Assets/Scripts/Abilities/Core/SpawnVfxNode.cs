using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
[AddTypeMenu("Эффекты/VFX на кастере")]
public class SpawnVfxNode : AbilityNode
{
    public GameObject vfxPrefab;

    [Tooltip("Прикрепить к трансформу кастера (двигается вместе с ним)")]
    public bool attachToOwner = true;

    [Tooltip("Убрать VFX при завершении или отмене каста; иначе он живёт lifetime секунд")]
    public bool destroyOnCastEnd = true;

    [Min(0f)] public float lifetime = 3f;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (vfxPrefab == null)
        {
            Debug.LogWarning("[SpawnVfxNode] vfxPrefab is not assigned.");
            return UniTask.FromResult(NodeResult.Failure);
        }

        Transform ownerTransform = ctx?.Owner?.Transform;
        if (ownerTransform == null)
        {
            return UniTask.FromResult(NodeResult.Failure);
        }

        GameObject vfx;
        if (attachToOwner)
        {
            vfx = UnityEngine.Object.Instantiate(vfxPrefab, ownerTransform);
            vfx.transform.localPosition = Vector3.zero;
        }
        else
        {
            vfx = UnityEngine.Object.Instantiate(vfxPrefab, ctx.Owner.Center, Quaternion.identity);
        }

        if (destroyOnCastEnd)
        {
            ctx.RegisterCleanup(() =>
            {
                if (vfx != null)
                {
                    UnityEngine.Object.Destroy(vfx);
                }
            });
        }
        else if (lifetime > 0f)
        {
            UnityEngine.Object.Destroy(vfx, lifetime);
        }

        return UniTask.FromResult(NodeResult.Success);
    }
}
