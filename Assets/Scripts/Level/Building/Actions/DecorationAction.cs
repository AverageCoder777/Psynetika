using System;
using UnityEngine;

// Слой просто рисуется: фон, дальний план, декор. Коллайдеров не создаёт.
[Serializable]
[AddTypeMenu("Локация/Только спрайт")]
public class DecorationAction : LocationLayerAction
{
    public DecorationAction()
    {
        renderSprite = true;
    }

    public override string Describe() => $"спрайт, sorting layer {LayerLabel(sortingLayer)}";

    public override void Apply(LocationLayerSource layer, LocationBuildContext ctx)
    {
        CreateObject(layer, ctx);
        ctx.Note("спрайт");
    }
}
