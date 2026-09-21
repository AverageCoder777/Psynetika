using System.Collections.Generic;
using UnityEngine;

/*
Сборка коллайдера по пиксельной маске слоя. Общая для всех действий, чтобы форма фигуры
(контур / прямоугольники / габариты) была одним и тем же кодом везде, где она нужна.

Outline  — один PolygonCollider2D, путь на каждый замкнутый контур. Дырки в геометрии Unity
           понимает сама: путь внутри другого пути вычитается.
Boxes    — Rigidbody2D (Static) + CompositeCollider2D + набор BoxCollider2D на одном объекте.
           Ровно та же схема, что в существующем Assets/Prefabs/level 1/Стены.prefab (там ещё старое usedByComposite).
BoundingBox — один прямоугольник по габаритам непрозрачных пикселей, с отступом в юнитах.
*/
public static class LocationColliderFactory
{
    public static Collider2D Build(
        GameObject target,
        SpriteMask2D mask,
        ColliderShapeMode shape,
        LocationBuildConfig config,
        bool isTrigger,
        float boundsPadding,
        out int pieces)
    {
        pieces = 0;

        if (target == null || mask == null || mask.SolidCount == 0)
        {
            return null;
        }

        switch (shape)
        {
            case ColliderShapeMode.Boxes:
                return BuildBoxes(target, mask, config, isTrigger, out pieces);

            case ColliderShapeMode.BoundingBox:
                return BuildBoundingBox(target, mask, boundsPadding, isTrigger, out pieces);

            default:
                return BuildOutline(target, mask, config, isTrigger, out pieces);
        }
    }

    private static Collider2D BuildOutline(
        GameObject target,
        SpriteMask2D mask,
        LocationBuildConfig config,
        bool isTrigger,
        out int pieces)
    {
        pieces = 0;

        List<List<Vector2>> loops = ContourTracer.Trace(mask, config.minContourArea);
        List<Vector2[]> paths = new(loops.Count);

        foreach (List<Vector2> loop in loops)
        {
            List<Vector2> simplified = PathSimplifier.Simplify(loop, config.simplifyTolerance, config.minPathPoints);

            if (simplified.Count < 3)
            {
                continue;
            }

            Vector2[] path = new Vector2[simplified.Count];

            for (int i = 0; i < simplified.Count; i++)
            {
                path[i] = mask.ToLocal(simplified[i].x, simplified[i].y);
            }

            paths.Add(path);
        }

        if (paths.Count == 0)
        {
            return null;
        }

        PolygonCollider2D collider = target.AddComponent<PolygonCollider2D>();
        collider.isTrigger = isTrigger;
        collider.pathCount = paths.Count;

        for (int i = 0; i < paths.Count; i++)
        {
            collider.SetPath(i, paths[i]);
        }

        pieces = paths.Count;

        return collider;
    }

    private static Collider2D BuildBoxes(
        GameObject target,
        SpriteMask2D mask,
        LocationBuildConfig config,
        bool isTrigger,
        out int pieces)
    {
        pieces = 0;

        List<RectInt> rects = RectDecomposer.Decompose(mask, config.minRectArea);

        if (rects.Count == 0)
        {
            return null;
        }

        Rigidbody2D body = target.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;

        CompositeCollider2D composite = target.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.isTrigger = isTrigger;

        foreach (RectInt rect in rects)
        {
            Vector2 min = mask.ToLocal(rect.xMin, rect.yMin);
            Vector2 max = mask.ToLocal(rect.xMax, rect.yMax);

            BoxCollider2D box = target.AddComponent<BoxCollider2D>();
            box.size = max - min;
            box.offset = (min + max) * 0.5f;
            box.compositeOperation = Collider2D.CompositeOperation.Merge;
        }

        composite.GenerateGeometry();
        pieces = rects.Count;

        return composite;
    }

    private static Collider2D BuildBoundingBox(
        GameObject target,
        SpriteMask2D mask,
        float padding,
        bool isTrigger,
        out int pieces)
    {
        pieces = 0;

        if (!RectDecomposer.TryGetBounds(mask, out RectInt bounds))
        {
            return null;
        }

        Vector2 min = mask.ToLocal(bounds.xMin, bounds.yMin) - new Vector2(padding, padding);
        Vector2 max = mask.ToLocal(bounds.xMax, bounds.yMax) + new Vector2(padding, padding);

        PolygonCollider2D collider = target.AddComponent<PolygonCollider2D>();
        collider.isTrigger = isTrigger;
        collider.pathCount = 1;
        collider.SetPath(0, new[]
        {
            new Vector2(min.x, min.y),
            new Vector2(max.x, min.y),
            new Vector2(max.x, max.y),
            new Vector2(min.x, max.y)
        });

        pieces = 1;

        return collider;
    }
}
