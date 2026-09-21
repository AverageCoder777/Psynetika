using System;
using UnityEngine;

/*
Односторонняя платформа: сквозь неё проходят снизу вверх и стоят сверху.

Всегда режется на прямоугольники: PlatformEffector2D действует на коллайдеры того же объекта,
а CompositeCollider2D как раз собирает набор боксов в один коллайдер с usedByEffector.
*/
[Serializable]
[AddTypeMenu("Локация/Односторонняя платформа")]
public class OneWayPlatformAction : LocationLayerAction
{
    [Tooltip("Слой физики объекта")]
    [PhysicsLayerName] public string physicsLayer = "Platform";

    [Tooltip("Тег объекта. Пусто = Untagged")]
    [TagName] public string tag = "Platform";

    [Tooltip("Сектор поверхности, град. 170 = проходит снизу и с боков, стоит только сверху")]
    [Range(1f, 360f)] public float surfaceArc = 170f;

    [Tooltip("Поворот сектора, град. 0 = платформа держит сверху")]
    public float rotationalOffset;

    public PhysicsMaterial2D physicsMaterial;

    public override string Describe() =>
        $"односторонняя платформа, слой {LayerLabel(physicsLayer)}, тег {TagLabel(tag)}";

    public override void Apply(LocationLayerSource layer, LocationBuildContext ctx)
    {
        GameObject target = CreateObject(layer, ctx);
        Collider2D collider = BuildCollider(target, layer, ctx, ColliderShapeMode.Boxes, false, 0f);

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

        PlatformEffector2D effector = target.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.surfaceArc = surfaceArc;
        effector.rotationalOffset = rotationalOffset;

        collider.usedByEffector = true;

        ctx.Note("PlatformEffector2D");
    }
}
