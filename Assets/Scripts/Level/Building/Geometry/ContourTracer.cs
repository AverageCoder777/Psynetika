using System.Collections.Generic;
using UnityEngine;

/*
Обводит непрозрачные пиксели маски замкнутыми контурами.

Работает не по «клеткам», а по рёбрам пикселей: у каждого непрозрачного пикселя берутся те стороны,
за которыми пусто, и ориентируются так, чтобы тело слоя всегда оставалось слева. Такие направленные
рёбра однозначно сшиваются в циклы — внешние границы получаются против часовой стрелки, дырки по
часовой. PolygonCollider2D именно так и понимает дырки: путь внутри другого пути вычитается.

Результат — координаты в пикселях (целые, на сетке рёбер). Упрощением занимается PathSimplifier.
*/
public static class ContourTracer
{
    // Приоритет поворота на развилке: сначала направо, потом прямо, потом налево.
    // Из-за этого пиксели, касающиеся по диагонали, считаются одним телом, а не двумя.
    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(-1, 0),
        new Vector2Int(0, -1)
    };

    public static List<List<Vector2>> Trace(SpriteMask2D mask, float minLoopArea)
    {
        List<List<Vector2>> result = new();

        if (mask == null || mask.SolidCount == 0)
        {
            return result;
        }

        Dictionary<Vector2Int, List<Vector2Int>> outgoing = CollectEdges(mask);
        List<Vector2Int> starts = new(outgoing.Keys);

        foreach (Vector2Int start in starts)
        {
            while (outgoing.TryGetValue(start, out List<Vector2Int> fromStart) && fromStart.Count > 0)
            {
                List<Vector2Int> loop = WalkLoop(outgoing, start);

                if (loop == null || loop.Count < 3)
                {
                    continue;
                }

                if (Mathf.Abs(SignedArea(loop)) < minLoopArea)
                {
                    continue;
                }

                List<Vector2> path = new(loop.Count);

                foreach (Vector2Int point in loop)
                {
                    path.Add(new Vector2(point.x, point.y));
                }

                result.Add(path);
            }
        }

        return result;
    }

    // Стороны непрозрачных пикселей, граничащие с пустотой, в обходе против часовой стрелки.
    private static Dictionary<Vector2Int, List<Vector2Int>> CollectEdges(SpriteMask2D mask)
    {
        Dictionary<Vector2Int, List<Vector2Int>> outgoing = new();

        for (int y = 0; y < mask.Height; y++)
        {
            for (int x = 0; x < mask.Width; x++)
            {
                if (!mask[x, y])
                {
                    continue;
                }

                if (!mask[x, y - 1])
                {
                    AddEdge(outgoing, new Vector2Int(x, y), new Vector2Int(x + 1, y));
                }

                if (!mask[x + 1, y])
                {
                    AddEdge(outgoing, new Vector2Int(x + 1, y), new Vector2Int(x + 1, y + 1));
                }

                if (!mask[x, y + 1])
                {
                    AddEdge(outgoing, new Vector2Int(x + 1, y + 1), new Vector2Int(x, y + 1));
                }

                if (!mask[x - 1, y])
                {
                    AddEdge(outgoing, new Vector2Int(x, y + 1), new Vector2Int(x, y));
                }
            }
        }

        return outgoing;
    }

    private static void AddEdge(Dictionary<Vector2Int, List<Vector2Int>> outgoing, Vector2Int from, Vector2Int to)
    {
        if (!outgoing.TryGetValue(from, out List<Vector2Int> list))
        {
            list = new List<Vector2Int>(2);
            outgoing[from] = list;
        }

        list.Add(to);
    }

    private static List<Vector2Int> WalkLoop(Dictionary<Vector2Int, List<Vector2Int>> outgoing, Vector2Int start)
    {
        List<Vector2Int> loop = new();
        Vector2Int current = start;
        Vector2Int direction = Vector2Int.zero;
        bool hasDirection = false;

        while (true)
        {
            if (!TakeNext(outgoing, current, direction, hasDirection, out Vector2Int next))
            {
                // Оборванная цепочка возможна только при повреждённых данных — контур отбрасываем.
                return loop.Count >= 3 ? loop : null;
            }

            loop.Add(current);
            direction = next - current;
            hasDirection = true;
            current = next;

            if (current == start)
            {
                return loop;
            }

            if (loop.Count > 4 * outgoing.Count + 8)
            {
                return null;
            }
        }
    }

    private static bool TakeNext(
        Dictionary<Vector2Int, List<Vector2Int>> outgoing,
        Vector2Int from,
        Vector2Int direction,
        bool hasDirection,
        out Vector2Int next)
    {
        next = default;

        if (!outgoing.TryGetValue(from, out List<Vector2Int> list) || list.Count == 0)
        {
            return false;
        }

        int chosen = 0;

        if (hasDirection && list.Count > 1)
        {
            int bestPriority = int.MaxValue;

            for (int i = 0; i < list.Count; i++)
            {
                int priority = TurnPriority(direction, list[i] - from);

                if (priority < bestPriority)
                {
                    bestPriority = priority;
                    chosen = i;
                }
            }
        }

        next = list[chosen];
        list.RemoveAt(chosen);

        return true;
    }

    // 0 — направо, 1 — прямо, 2 — налево, 3 — разворот.
    private static int TurnPriority(Vector2Int incoming, Vector2Int candidate)
    {
        Vector2Int right = new Vector2Int(incoming.y, -incoming.x);

        if (candidate == right)
        {
            return 0;
        }

        if (candidate == incoming)
        {
            return 1;
        }

        if (candidate == -right)
        {
            return 2;
        }

        return 3;
    }

    // Положительная площадь — внешний контур, отрицательная — дырка.
    public static float SignedArea(IReadOnlyList<Vector2Int> loop)
    {
        float area = 0f;

        for (int i = 0; i < loop.Count; i++)
        {
            Vector2Int a = loop[i];
            Vector2Int b = loop[(i + 1) % loop.Count];
            area += a.x * b.y - b.x * a.y;
        }

        return area * 0.5f;
    }
}
