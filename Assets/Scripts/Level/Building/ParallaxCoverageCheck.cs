using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

/*
Не открывает ли параллакс пустоту в кадре.

Один слой сам по себе дыру не покажет: при якоре в центре слоя и коэффициенте 0..1 он закрывает
кадр везде, где закрывал без параллакса. Дыры даёт композиция — слои съезжают друг относительно
друга, и за краем горы, который раньше прятала земля, оказывается небо, которого там не нарисовали.

Поэтому проверка гоняет камеру по сетке положений в пределах уровня и в каждом сравнивает кадр
«как нарисовано» с кадром «с параллаксом»: точка, закрытая артом без параллакса и пустая с ним, —
дыра. Пустоты, которые были и без параллакса, не в счёт: это решение художника, а не поломка.

Всё считается в локальных единицах корня, по маскам слоёв из кеша контекста.
*/
public static class ParallaxCoverageCheck
{
    private const int CameraStepsX = 24;
    private const int CameraStepsY = 8;
    private const int ScreenStepsX = 32;
    private const int ScreenStepsY = 18;

    // Слой, который рисуется в кадре: его маска и как он сдвигается за камерой.
    public class Layer
    {
        public string name;
        public SpriteMask2D mask;
        public Vector2 anchor;
        public Vector2 factor;
    }

    public static void Run(List<Layer> layers, LocationBuildContext ctx)
    {
        if (!layers.Any(layer => layer.factor != Vector2.zero))
        {
            return;
        }

        if (!ctx.HasLevelBounds)
        {
            return;
        }

        if (!ctx.TryGetViewHalfExtents(out Vector2 half))
        {
            ctx.Report.Info("Параллакс: CinemachineCamera с конфайнером не найдена, размер кадра неизвестен — проверка пустот пропущена");
            return;
        }

        GetCameraArea(ctx, out Vector2 areaMin, out Vector2 areaMax);

        Vector2 cameraMin = areaMin + half;
        Vector2 cameraMax = areaMax - half;
        Vector2 levelCenter = (areaMin + areaMax) * 0.5f;

        // Уровень меньше кадра по оси — камера по ней не ходит, стоит по центру.
        if (cameraMin.x > cameraMax.x)
        {
            cameraMin.x = cameraMax.x = levelCenter.x;
        }

        if (cameraMin.y > cameraMax.y)
        {
            cameraMin.y = cameraMax.y = levelCenter.y;
        }

        int positions = 0;
        int badPositions = 0;
        int worstHoles = 0;
        Vector2 worstCamera = Vector2.zero;
        Dictionary<string, int> culprits = new();

        for (int cy = 0; cy < CameraStepsY; cy++)
        {
            for (int cx = 0; cx < CameraStepsX; cx++)
            {
                Vector2 camera = new(
                    Mathf.Lerp(cameraMin.x, cameraMax.x, cx / (CameraStepsX - 1f)),
                    Mathf.Lerp(cameraMin.y, cameraMax.y, cy / (CameraStepsY - 1f)));

                int holes = CountHoles(layers, camera, half, culprits);
                positions++;

                if (holes == 0)
                {
                    continue;
                }

                badPositions++;

                if (holes > worstHoles)
                {
                    worstHoles = holes;
                    worstCamera = camera;
                }
            }
        }

        if (badPositions == 0)
        {
            ctx.Report.Info("Параллакс: пустот в кадре не найдено");
            return;
        }

        Vector3 worstWorld = ctx.Root.TransformPoint(worstCamera);
        float worstShare = worstHoles * 100f / (ScreenStepsX * ScreenStepsY);
        string suspects = string.Join(", ", culprits
            .OrderByDescending(pair => pair.Value)
            .Take(3)
            .Select(pair => $"«{pair.Key}»"));

        ctx.Warn(string.Format(
            CultureInfo.InvariantCulture,
            "Параллакс открывает пустоту: в {0} из {1} положений камеры в кадре видны места без арта " +
            "(хуже всего — камера около ({2:0.#}, {3:0.#}), {4:0}% кадра). Съезжают слои: {5}. " +
            "Дорисуйте их к краям или уменьшите коэффициент",
            badPositions, positions, worstWorld.x, worstWorld.y, worstShare, suspects));
    }

    /*
    Где камера может оказаться: границы конфайнера (cam_*, путь камеры или прямоугольник по габаритам),
    иначе — габариты всех слоёв. Без этого небо, нарисованное с запасом над уровнем, давало бы
    ложные тревоги в местах, куда камера никогда не заедет.
    */
    private static void GetCameraArea(LocationBuildContext ctx, out Vector2 min, out Vector2 max)
    {
        min = ctx.LevelMin;
        max = ctx.LevelMax;

        Collider2D shape = ctx.Confiner != null ? ctx.Confiner.BoundingShape2D : null;

        if (shape == null)
        {
            return;
        }

        // Свежесозданный в редакторе коллайдер получает габариты только после синхронизации физики.
        Physics2D.SyncTransforms();
        Bounds bounds = shape.bounds;

        if (bounds.size.x <= 0f || bounds.size.y <= 0f)
        {
            return;
        }

        min = ctx.Root.InverseTransformPoint(bounds.min);
        max = ctx.Root.InverseTransformPoint(bounds.max);
    }

    private static int CountHoles(List<Layer> layers, Vector2 camera, Vector2 half, Dictionary<string, int> culprits)
    {
        int holes = 0;

        for (int sy = 0; sy < ScreenStepsY; sy++)
        {
            for (int sx = 0; sx < ScreenStepsX; sx++)
            {
                // Центры ячеек сетки, чтобы не попадать ровно в край кадра.
                Vector2 point = camera + new Vector2(
                    ((sx + 0.5f) / ScreenStepsX * 2f - 1f) * half.x,
                    ((sy + 0.5f) / ScreenStepsY * 2f - 1f) * half.y);

                if (!CoveredAsDrawn(layers, point) || CoveredWithParallax(layers, point, camera))
                {
                    continue;
                }

                holes++;

                // Виноваты слои, которые закрывали точку, но уехали: у неподвижных смещения нет.
                foreach (Layer layer in layers)
                {
                    if (layer.factor != Vector2.zero && layer.mask.IsSolidAt(point))
                    {
                        culprits[layer.name] = culprits.TryGetValue(layer.name, out int count) ? count + 1 : 1;
                    }
                }
            }
        }

        return holes;
    }

    private static bool CoveredAsDrawn(List<Layer> layers, Vector2 point)
    {
        foreach (Layer layer in layers)
        {
            if (layer.mask.IsSolidAt(point))
            {
                return true;
            }
        }

        return false;
    }

    // Та же формула, что у ParallaxLayer: слой сдвинут на (камера − якорь) × k.
    private static bool CoveredWithParallax(List<Layer> layers, Vector2 point, Vector2 camera)
    {
        foreach (Layer layer in layers)
        {
            Vector2 offset = Vector2.Scale(camera - layer.anchor, layer.factor);

            if (layer.mask.IsSolidAt(point - offset))
            {
                return true;
            }
        }

        return false;
    }
}
