using System.Collections.Generic;
using UnityEngine;

/*
Упрощение контура, полученного от ContourTracer.

Сырой контур пиксель-арта состоит из единичных отрезков: наклонная линия даёт сотни точек-ступенек,
а прямая стена — по точке на каждый пиксель. В коллайдер такое класть нельзя, поэтому:

1. выкидываются точки на одной прямой (убирает длинные прямые участки без потери формы);
2. Ramer–Douglas–Peucker с порогом в пикселях сглаживает ступеньки в наклонные отрезки.

Замкнутость контура учитывается: кольцо режется на две половины по паре самых далёких друг от друга
точек, иначе RDP «съел» бы угол рядом с произвольной начальной точкой.
*/
public static class PathSimplifier
{
    private const float CollinearEpsilon = 0.0001f;

    public static List<Vector2> Simplify(IReadOnlyList<Vector2> loop, float tolerance, int minPoints)
    {
        List<Vector2> cleaned = RemoveCollinear(loop);

        if (cleaned.Count <= minPoints || tolerance <= 0f)
        {
            return cleaned;
        }

        int anchor = FarthestFromCentroid(cleaned);
        int opposite = FarthestFromPoint(cleaned, anchor);

        if (anchor == opposite)
        {
            return cleaned;
        }

        List<Vector2> firstHalf = Slice(cleaned, anchor, opposite);
        List<Vector2> secondHalf = Slice(cleaned, opposite, anchor);

        List<Vector2> result = new();
        AppendSimplified(firstHalf, tolerance, result);
        AppendSimplified(secondHalf, tolerance, result);

        return result.Count >= minPoints ? result : cleaned;
    }

    private static List<Vector2> RemoveCollinear(IReadOnlyList<Vector2> loop)
    {
        List<Vector2> result = new(loop.Count);
        int count = loop.Count;

        for (int i = 0; i < count; i++)
        {
            Vector2 current = loop[i];
            Vector2 next = loop[(i + 1) % count];

            if (result.Count == 0)
            {
                result.Add(current);
                continue;
            }

            Vector2 previous = result[result.Count - 1];

            if (!IsCollinear(previous, current, next))
            {
                result.Add(current);
            }
        }

        // Стартовая точка выбиралась вслепую и тоже может оказаться на прямой.
        while (result.Count > 3 && IsCollinear(result[result.Count - 1], result[0], result[1]))
        {
            result.RemoveAt(0);
        }

        return result;
    }

    private static bool IsCollinear(Vector2 previous, Vector2 current, Vector2 next)
    {
        Vector2 a = current - previous;
        Vector2 b = next - current;

        return Mathf.Abs(a.x * b.y - a.y * b.x) < CollinearEpsilon;
    }

    private static int FarthestFromCentroid(IReadOnlyList<Vector2> points)
    {
        Vector2 centroid = Vector2.zero;

        foreach (Vector2 point in points)
        {
            centroid += point;
        }

        centroid /= points.Count;

        return FarthestFrom(points, centroid);
    }

    private static int FarthestFromPoint(IReadOnlyList<Vector2> points, int index)
    {
        return FarthestFrom(points, points[index]);
    }

    private static int FarthestFrom(IReadOnlyList<Vector2> points, Vector2 origin)
    {
        int best = 0;
        float bestDistance = -1f;

        for (int i = 0; i < points.Count; i++)
        {
            float distance = (points[i] - origin).sqrMagnitude;

            if (distance > bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }

    // Участок кольца от from до to включительно, с переходом через конец списка.
    private static List<Vector2> Slice(IReadOnlyList<Vector2> loop, int from, int to)
    {
        List<Vector2> result = new();
        int count = loop.Count;
        int index = from;

        while (true)
        {
            result.Add(loop[index]);

            if (index == to)
            {
                break;
            }

            index = (index + 1) % count;
        }

        return result;
    }

    // Добавляет упрощённую половину без последней точки: её добавит следующая половина.
    private static void AppendSimplified(List<Vector2> polyline, float tolerance, List<Vector2> result)
    {
        if (polyline.Count <= 2)
        {
            result.Add(polyline[0]);
            return;
        }

        List<int> kept = new();
        ReduceRecursive(polyline, 0, polyline.Count - 1, tolerance, kept);

        result.Add(polyline[0]);

        foreach (int index in kept)
        {
            result.Add(polyline[index]);
        }
    }

    private static void ReduceRecursive(List<Vector2> points, int first, int last, float tolerance, List<int> kept)
    {
        if (last <= first + 1)
        {
            return;
        }

        float maxDistance = -1f;
        int farthest = -1;

        for (int i = first + 1; i < last; i++)
        {
            float distance = PerpendicularDistance(points[i], points[first], points[last]);

            if (distance > maxDistance)
            {
                maxDistance = distance;
                farthest = i;
            }
        }

        if (farthest < 0 || maxDistance <= tolerance)
        {
            return;
        }

        ReduceRecursive(points, first, farthest, tolerance, kept);
        kept.Add(farthest);
        ReduceRecursive(points, farthest, last, tolerance, kept);
    }

    private static float PerpendicularDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 line = lineEnd - lineStart;
        float lengthSquared = line.sqrMagnitude;

        if (lengthSquared < CollinearEpsilon)
        {
            return (point - lineStart).magnitude;
        }

        float projection = Mathf.Clamp01(Vector2.Dot(point - lineStart, line) / lengthSquared);
        Vector2 closest = lineStart + line * projection;

        return (point - closest).magnitude;
    }
}
