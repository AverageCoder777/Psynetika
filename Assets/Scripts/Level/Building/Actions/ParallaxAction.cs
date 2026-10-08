using System;
using System.Globalization;
using UnityEngine;

/*
Слой с параллаксом: рисуется как обычный спрайт и получает ParallaxLayer, который в игре
сдвигает его вслед за камерой. Коэффициенты — по осям отдельно, смысл тот же, что у ParallaxLayer:
0 — стоит в мире, 1 — приклеен к камере, 0..1 — фон, меньше 0 — передний план.

Не открывает ли параллакс пустоту в кадре, проверяется после сборки всех слоёв сразу
(ParallaxCoverageCheck): дыру даёт только композиция, один слой сам по себе её не покажет.
*/
[Serializable]
[AddTypeMenu("Локация/Параллакс")]
public class ParallaxAction : DecorationAction
{
    [Tooltip("Коэффициент по горизонтали: 0 — стоит в мире, 1 — приклеен к камере, " +
             "0..1 — фон (ближе к 1 — дальше), меньше 0 — передний план")]
    [Range(-1f, 1f)] public float horizontal = 0.5f;

    [Tooltip("Коэффициент по вертикали, смысл тот же. По умолчанию 0: фон не спускается и не поднимается " +
             "вслед за камерой, а стоит по высоте как нарисован")]
    [Range(-1f, 1f)] public float vertical;

    public Vector2 Factor => new(horizontal, vertical);

    public override string Describe() => $"параллакс {FactorLabel}, sorting layer {LayerLabel(sortingLayer)}";

    public override void Apply(LocationLayerSource layer, LocationBuildContext ctx)
    {
        GameObject target = CreateObject(layer, ctx);
        target.AddComponent<ParallaxLayer>().factor = Factor;

        ctx.Note($"спрайт, параллакс {FactorLabel}");
    }

    private string FactorLabel => string.Format(CultureInfo.InvariantCulture, "{0:0.##} × {1:0.##}", horizontal, vertical);
}
