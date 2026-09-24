using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

/*
Границы камеры: слой превращается в коллайдер-фигуру и подставляется в CinemachineConfiner2D.

По умолчанию берутся габариты слоя — камере почти всегда нужен простой прямоугольник, а сложная
фигура заставляет конфайнер каждый раз перепекать решение. Outline нужен там, где уровень
непрямоугольный и камера должна ходить по «коридору».
*/
[Serializable]
[AddTypeMenu("Локация/Границы камеры")]
public class CameraBoundsAction : LocationLayerAction
{
    [Tooltip("BoundingBox — прямоугольник по габаритам слоя, Outline — точная фигура")]
    public ColliderShapeMode shape = ColliderShapeMode.BoundingBox;

    [Tooltip("Отступ границ наружу, в юнитах. Отрицательный — внутрь")]
    public float padding;

    [Tooltip("Слой физики объекта границ")]
    [PhysicsLayerName] public string physicsLayer = "Ignore Raycast";

    public override string Describe() => "границы камеры → CinemachineConfiner2D";

    public override void Apply(LocationLayerSource layer, LocationBuildContext ctx)
    {
        GameObject target = CreateObject(layer, ctx);
        // Конфайнеру нужна ровно одна фигура: отдельные боксы здесь сливаются в одну.
        ColliderShapeMode resolvedShape = shape == ColliderShapeMode.Boxes ? ColliderShapeMode.MergedBoxes : shape;
        List<Collider2D> colliders = BuildCollider(target, layer, ctx, resolvedShape, true, padding);

        if (colliders.Count == 0)
        {
            return;
        }

        if (resolvedShape != shape)
        {
            ctx.Note("боксы слиты для конфайнера");
        }

        Collider2D collider = colliders[0];

        target.layer = ctx.ResolveLayer(physicsLayer, layer.Name);

        CinemachineConfiner2D confiner = ctx.Confiner;

        if (confiner == null)
        {
            ctx.Warn($"Слой «{layer.Name}»: границы построены, но CinemachineConfiner2D в сцене не найден — подключите вручную");
            return;
        }

        confiner.BoundingShape2D = collider;
        confiner.InvalidateBoundingShapeCache();

        ctx.Note($"подключено к {confiner.name}");
    }
}
