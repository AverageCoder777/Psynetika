using System;
using System.Collections.Generic;
using UnityEngine;

/*
Коридор вдоль пути: из линии, нарисованной в сцене, получается замкнутая фигура,
которую понимает CinemachineConfiner2D.

Почему не одна вычисленная руками оболочка: честное смещение контура наружу ломается на острых
углах и на самопересечениях пути, а уровень как раз и рисуют «змейкой». Поэтому фигура собирается
объединением простых кусков, а объединяет их сам Unity (CompositeCollider2D, GeometryType.Polygons):
  отрезок — четырёхугольник: полоса шириной в кадр вокруг отрезка, край которой дополнительно
            приподнят на up и опущен на down своих концов;
  узел    — прямоугольник не меньше кадра камеры и не меньше квадрата со стороной в ширину
            коридора, вытянутый на up и down.

Почему узел не просто квадрат в ширину коридора (он описан вокруг круга радиуса width/2 и
накрывает внешний клин поворота): конфайнер пускает центр камеры только туда, где помещается
весь кадр. На остром повороте («галочка» вниз) нижние края соседних полос уходят вверх по
диагонали, и широкий кадр 16:9 упирается в них углами задолго до точки поворота — камера висит
над дном. Узел размером с кадр гарантирует, что камера может встать центром ровно в точку пути.

Геометрия кусков открыта (SegmentPolygon, JointPolygon): тем же кодом редактор рисует превью,
поэтому в Scene View видна ровно та фигура, которая потом станет коллайдером.

Зум точки (CameraPathPoint.zoom) растягивает коридор вместе с обзором: ширина рассчитана под кадр
обычного размера, а в отдалённом месте кадр больше — узкий коридор выталкивал бы камеру с линии.
Поэтому полоса отрезка — трапеция: у каждого конца своя ширина, умноженная на зум этого конца.
*/
public static class CameraCorridorFactory
{
    public const string SegmentPrefix = "Отрезок";
    public const string JointPrefix = "Узел";

    public static Collider2D Build(
        GameObject target,
        IReadOnlyList<CameraPathPoint> samples,
        Func<Vector2, float> widthForDirection,
        bool closed,
        bool isTrigger = true)
    {
        if (target == null || samples == null || samples.Count < 2 || widthForDirection == null)
        {
            return null;
        }

        if (!target.TryGetComponent(out Rigidbody2D body))
        {
            body = target.AddComponent<Rigidbody2D>();
        }

        body.bodyType = RigidbodyType2D.Static;

        CompositeCollider2D composite = target.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.isTrigger = isTrigger;

        float[] widths = SegmentWidths(samples, widthForDirection, closed);
        int built = 0;

        for (int i = 0; i < widths.Length; i++)
        {
            if (widths[i] <= 0f)
            {
                continue;
            }

            CameraPathPoint from = samples[i];
            CameraPathPoint to = samples[(i + 1) % samples.Count];

            AddPolygon(CreateChild(target, $"{SegmentPrefix} {i}"), SegmentPolygon(from, to, widths[i]));
            built++;
        }

        if (built == 0)
        {
            return null;
        }

        // На концах незамкнутого пути узлов нет: коридор должен кончаться ровно на точке,
        // иначе камера уезжает за край уровня на половину ширины.
        GetJointRange(samples.Count, closed, out int first, out int last);
        Vector2 frame = FrameSize(widthForDirection);

        for (int i = first; i <= last; i++)
        {
            Vector2 size = JointSize(widths, i, frame);

            if (size.x > 0f)
            {
                AddPolygon(CreateChild(target, $"{JointPrefix} {i}"), JointPolygon(samples[i], size));
            }
        }

        composite.GenerateGeometry();

        return composite;
    }

    // Ширина коридора на каждом отрезке; 0 — отрезок нулевой длины, его пропускаем.
    public static float[] SegmentWidths(
        IReadOnlyList<CameraPathPoint> samples,
        Func<Vector2, float> widthForDirection,
        bool closed)
    {
        int segments = closed ? samples.Count : samples.Count - 1;
        float[] widths = new float[Mathf.Max(segments, 0)];

        for (int i = 0; i < segments; i++)
        {
            Vector2 delta = samples[(i + 1) % samples.Count].position - samples[i].position;

            if (delta.sqrMagnitude > Mathf.Epsilon)
            {
                widths[i] = Mathf.Max(0.01f, widthForDirection(delta));
            }
        }

        return widths;
    }

    /*
    Кадр камеры в тех же единицах, что ширина коридора: поперёк вертикального участка нужна ширина
    кадра, поперёк горизонтального — высота. Так размер берётся из той же функции ширины (с запасом
    slack), и при ручной ширине кадр получается квадратом — ровно как раньше.
    */
    public static Vector2 FrameSize(Func<Vector2, float> widthForDirection)
    {
        return new Vector2(widthForDirection(Vector2.up), widthForDirection(Vector2.right));
    }

    // Размер узла: не меньше кадра и не меньше квадрата в ширину соседних полос. 0 — узла нет.
    public static Vector2 JointSize(IReadOnlyList<float> widths, int vertex, Vector2 frame)
    {
        float side = JointWidth(widths, vertex);

        return side > 0f
            ? new Vector2(Mathf.Max(side, frame.x), Mathf.Max(side, frame.y))
            : Vector2.zero;
    }

    public static void GetJointRange(int sampleCount, bool closed, out int first, out int last)
    {
        first = closed ? 0 : 1;
        last = closed ? sampleCount - 1 : sampleCount - 2;
    }

    // Узел должен накрыть клин у обоих своих отрезков, поэтому берётся больший из них.
    public static float JointWidth(IReadOnlyList<float> widths, int vertex)
    {
        int segments = widths.Count;

        if (segments == 0)
        {
            return 0f;
        }

        int incoming = (vertex - 1 + segments) % segments;
        int outgoing = vertex % segments;

        return Mathf.Max(widths[incoming], widths[outgoing]);
    }

    /*
    Полоса вокруг отрезка. Нормаль всегда смотрит вверх (n.y >= 0), чтобы запас up расширял
    именно верхний край, в какую бы сторону ни шёл путь.
    */
    public static Vector2[] SegmentPolygon(CameraPathPoint from, CameraPathPoint to, float width)
    {
        Vector2 delta = to.position - from.position;
        Vector2 normal = new Vector2(-delta.y, delta.x).normalized;

        if (normal.y < 0f || (Mathf.Approximately(normal.y, 0f) && normal.x > 0f))
        {
            normal = -normal;
        }

        Vector2 halfFrom = normal * (width * from.Zoom * 0.5f);
        Vector2 halfTo = normal * (width * to.Zoom * 0.5f);

        return new[]
        {
            from.position - halfFrom - Vector2.up * from.down,
            to.position - halfTo - Vector2.up * to.down,
            to.position + halfTo + Vector2.up * to.up,
            from.position + halfFrom + Vector2.up * from.up
        };
    }

    public static Vector2[] JointPolygon(CameraPathPoint point, Vector2 size)
    {
        Vector2 half = size * (point.Zoom * 0.5f);
        Vector2 center = point.position;

        return new[]
        {
            center + new Vector2(-half.x, -half.y - point.down),
            center + new Vector2(half.x, -half.y - point.down),
            center + new Vector2(half.x, half.y + point.up),
            center + new Vector2(-half.x, half.y + point.up)
        };
    }

    private static GameObject CreateChild(GameObject parent, string name)
    {
        GameObject created = new(name);
        created.transform.SetParent(parent.transform, false);
        created.layer = parent.layer;

        return created;
    }

    // Коллайдеры детей висят на статичном Rigidbody2D родителя, поэтому композит их и собирает.
    private static void AddPolygon(GameObject target, Vector2[] path)
    {
        PolygonCollider2D polygon = target.AddComponent<PolygonCollider2D>();
        polygon.pathCount = 1;
        polygon.SetPath(0, path);
        polygon.compositeOperation = Collider2D.CompositeOperation.Merge;
    }
}
