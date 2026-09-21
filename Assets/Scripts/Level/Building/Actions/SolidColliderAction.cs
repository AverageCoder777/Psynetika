using System;
using UnityEngine;

// Непробиваемая геометрия: земля, стены, потолки.
[Serializable]
[AddTypeMenu("Локация/Статичная коллизия")]
public class SolidColliderAction : LocationLayerAction
{
    [Tooltip("Outline — контур по пикселям, склоны остаются наклонными. Boxes — прямоугольники через CompositeCollider2D")]
    public ColliderShapeMode shape = ColliderShapeMode.Outline;

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
            ColliderShapeMode.BoundingBox => "габариты",
            _ => "контур"
        };

        return $"коллизия ({shapeLabel}), слой {LayerLabel(physicsLayer)}, тег {TagLabel(tag)}";
    }

    public override void Apply(LocationLayerSource layer, LocationBuildContext ctx)
    {
        GameObject target = CreateObject(layer, ctx);
        Collider2D collider = BuildCollider(target, layer, ctx, shape, false, 0f);

        if (collider == null)
        {
            return;
        }

        target.layer = ctx.ResolveLayer(physicsLayer, layer.Name);
        ctx.ApplyTag(target, tag, layer.Name);

        if (physicsMaterial != null)
        {
            collider.sharedMaterial = physicsMaterial;
        }
    }
}
