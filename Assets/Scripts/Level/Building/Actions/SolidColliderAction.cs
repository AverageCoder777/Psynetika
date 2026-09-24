using System;
using System.Collections.Generic;
using UnityEngine;

// Непробиваемая геометрия: земля, стены, потолки.
[Serializable]
[AddTypeMenu("Локация/Статичная коллизия")]
public class SolidColliderAction : LocationLayerAction
{
    [Tooltip("Boxes — отдельные BoxCollider2D. Merged Boxes — те же прямоугольники, слитые CompositeCollider2D " +
             "(гладкие стыки). Outline — контур по пикселям, склоны остаются наклонными")]
    public ColliderShapeMode shape = ColliderShapeMode.Boxes;

    [Tooltip("Слой физики объекта: Floor, Walls, Up Walls…")]
    [PhysicsLayerName] public string physicsLayer = "Floor";

    [Tooltip("Тег объекта. Пусто = Untagged")]
    [TagName] public string tag = "Floor";

    public PhysicsMaterial2D physicsMaterial;

    public override string Describe()
    {
        string shapeLabel = shape switch
        {
            ColliderShapeMode.Boxes => "прямоугольники",
            ColliderShapeMode.MergedBoxes => "прямоугольники (слитые)",
            ColliderShapeMode.BoundingBox => "габариты",
            _ => "контур"
        };

        return $"коллизия ({shapeLabel}), слой {LayerLabel(physicsLayer)}, тег {TagLabel(tag)}";
    }

    public override void Apply(LocationLayerSource layer, LocationBuildContext ctx)
    {
        GameObject target = CreateObject(layer, ctx);
        List<Collider2D> colliders = BuildCollider(target, layer, ctx, shape, false, 0f);

        if (colliders.Count == 0)
        {
            return;
        }

        target.layer = ctx.ResolveLayer(physicsLayer, layer.Name);
        ctx.ApplyTag(target, tag, layer.Name);

        if (physicsMaterial != null)
        {
            foreach (Collider2D collider in colliders)
            {
                collider.sharedMaterial = physicsMaterial;
            }
        }
    }
}
