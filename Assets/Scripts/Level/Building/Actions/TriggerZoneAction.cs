using System;
using UnityEngine;

// Область-триггер без собственного поведения: заготовка под зоны, которые дообвешивают руками.
[Serializable]
[AddTypeMenu("Локация/Триггер-зона")]
public class TriggerZoneAction : LocationLayerAction
{
    [Tooltip("Merged Boxes — прямоугольники одной фигурой, Outline — точная форма, BoundingBox — один прямоугольник " +
             "по габаритам. Boxes (отдельные боксы) шлёт OnTriggerEnter2D от каждого бокса")]
    public ColliderShapeMode shape = ColliderShapeMode.MergedBoxes;

    [Tooltip("Слой физики объекта. Пусто = Default")]
    [PhysicsLayerName] public string physicsLayer = string.Empty;

    [Tooltip("Тег объекта. Пусто = Untagged")]
    [TagName] public string tag = string.Empty;

    public override string Describe() => $"триггер-зона, тег {TagLabel(tag)}";

    public override void Apply(LocationLayerSource layer, LocationBuildContext ctx)
    {
        GameObject target = CreateObject(layer, ctx);
        if (BuildCollider(target, layer, ctx, shape, true, 0f).Count == 0)
        {
            return;
        }

        target.layer = ctx.ResolveLayer(physicsLayer, layer.Name);
        ctx.ApplyTag(target, tag, layer.Name);

        Configure(target, ctx);
    }

    // Точка расширения для зон с поведением: см. DeadZoneAction.
    protected virtual void Configure(GameObject target, LocationBuildContext ctx)
    {
    }
}
