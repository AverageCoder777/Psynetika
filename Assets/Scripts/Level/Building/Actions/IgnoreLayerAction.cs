using System;

// Слой не попадает в сцену вовсе: черновики, подсказки художника, временная разметка.
[Serializable]
[AddTypeMenu("Локация/Пропустить")]
public class IgnoreLayerAction : LocationLayerAction
{
    public override string Describe() => "пропускается";

    public override void Apply(LocationLayerSource layer, LocationBuildContext ctx)
    {
        ctx.Note("пропущен");
    }
}
