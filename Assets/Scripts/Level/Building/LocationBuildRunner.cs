using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

/*
Исполнитель сборки локации. Не знает ни про редактор, ни про окно утилиты: принимает model prefab
.aseprite-файла, конфиг и корень в сцене, возвращает отчёт. Undo, меню и диалоги — дело Editor-части.

Слои читаются из model prefab, который AsepriteImporter генерирует при layerImportMode =
Individual Layers: на каждый слой там создан GameObject с именем слоя и SpriteRenderer, а группы
Aseprite дают вложенность. При Merge Frame слой ровно один, и собирать нечего — это ошибка
с понятным текстом, а не тихо пустой результат.
*/
public static class LocationBuildRunner
{
    public const string DefaultRootName = "Локация (сгенерировано)";
    public const string FallbackCameraBoundsName = "CameraBounds (по габаритам уровня)";

    // Подпись правила в отчёте для слоя, роль которого назначена руками в окне утилиты.
    public const string ManualRuleLabel = "вручную";

    // Разбор источника без создания объектов: для таблицы предпросмотра в окне.
    public static LocationBuildReport Preview(
        GameObject source,
        LocationBuildConfig config,
        List<LocationLayerOverride> overrides)
    {
        LocationBuildReport report = new();
        List<LocationLayerSource> layers = CollectLayers(source, report);

        if (report.HasErrors)
        {
            return report;
        }

        if (config == null)
        {
            report.Error("Не задан конфиг правил (LocationBuildConfig)");
            return report;
        }

        FillLevelSize(layers, config, report);

        foreach (LocationLayerSource layer in layers)
        {
            LocationLayerAction action = ResolveAction(layer.Name, config, overrides, out string ruleLabel, out bool matched);
            LocationBuildEntry entry = report.AddEntry(layer.Name, ruleLabel, matched);
            entry.result = action != null ? action.Describe() : "действие не задано";
        }

        return report;
    }

    public static LocationBuildReport Build(
        GameObject source,
        LocationBuildConfig config,
        List<LocationLayerOverride> overrides,
        Transform root,
        CinemachineConfiner2D confiner,
        Action<GameObject> objectCreated)
    {
        LocationBuildReport report = new();
        List<LocationLayerSource> layers = CollectLayers(source, report);

        if (report.HasErrors)
        {
            return report;
        }

        if (config == null)
        {
            report.Error("Не задан конфиг правил (LocationBuildConfig)");
            return report;
        }

        if (root == null)
        {
            report.Error("Не задан корень сборки в сцене");
            return report;
        }

        FillLevelSize(layers, config, report);

        // Масштаб корня — единственное место, где локация подгоняется под рост персонажа.
        // Спрайты рисуются по своему PPU, поэтому арт и коллайдеры можно растянуть только вместе.
        root.localScale = Vector3.one * config.worldScale;

        LocationBuildContext context = new(root, config, report, confiner, objectCreated);
        bool cameraBoundsBuilt = false;

        foreach (LocationLayerSource layer in layers)
        {
            LocationLayerAction action = ResolveAction(layer.Name, config, overrides, out string ruleLabel, out bool matched);
            context.CurrentEntry = report.AddEntry(layer.Name, ruleLabel, matched);

            if (action == null)
            {
                report.Warn($"Слой «{layer.Name}»: у правила «{ruleLabel}» не выбрано действие");
                context.Note("действие не задано");

                continue;
            }

            action.Apply(layer, context);

            if (action is CameraBoundsAction)
            {
                cameraBoundsBuilt = true;
            }
        }

        context.CurrentEntry = null;

        if (!cameraBoundsBuilt && !UsesCameraPath(context))
        {
            BuildFallbackCameraBounds(layers, context);
        }

        return report;
    }

    /*
    Роль слоя: сперва ручное назначение из окна утилиты, потом правила конфига.

    Порядок именно такой, потому что назначение ставят ровно тогда, когда имя слоя под правило
    не подходит: правило по маске не должно перебивать явный выбор человека.
    */
    public static LocationLayerAction ResolveAction(
        string layerName,
        LocationBuildConfig config,
        List<LocationLayerOverride> overrides,
        out string ruleLabel,
        out bool matched)
    {
        LocationLayerAction manual = LocationLayerOverride.Find(overrides, layerName);

        if (manual != null)
        {
            ruleLabel = ManualRuleLabel;
            matched = true;

            return manual;
        }

        return config.ResolveAction(layerName, out ruleLabel, out matched);
    }

    /*
    Границы камеры уже заданы путём камеры (CameraPath) — прямоугольник по габаритам уровня
    не строим, иначе сборка арта каждый раз затирала бы коридор, проложенный дизайнером.
    */
    private static bool UsesCameraPath(LocationBuildContext context)
    {
        CinemachineConfiner2D confiner = context.Confiner;

        if (confiner == null || confiner.BoundingShape2D == null)
        {
            return false;
        }

        CameraPath path = confiner.BoundingShape2D.GetComponentInParent<CameraPath>();

        if (path == null)
        {
            return false;
        }

        context.Report.Info($"Границы камеры взяты из пути камеры «{path.name}» — прямоугольник по габаритам не строился");

        return true;
    }

    public static List<LocationLayerSource> CollectLayers(GameObject source, LocationBuildReport report)
    {
        List<LocationLayerSource> layers = new();

        if (source == null)
        {
            report.Error("Не задан исходный .aseprite");
            return layers;
        }

        if (source.transform.childCount == 0)
        {
            report.Error(
                $"«{source.name}» импортирован одним слоем: у модели нет дочерних объектов. " +
                "Переключите импорт файла на Individual Layers.");

            return layers;
        }

        Collect(source.transform, string.Empty, layers);

        if (layers.Count == 0)
        {
            report.Error($"В «{source.name}» не нашлось ни одного слоя со спрайтом");
        }

        return layers;
    }

    private static void Collect(Transform parent, string groupPath, List<LocationLayerSource> layers)
    {
        foreach (Transform child in parent)
        {
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();

            if (renderer != null && renderer.sprite != null)
            {
                layers.Add(new LocationLayerSource(child.name, groupPath, renderer.sprite, renderer.sortingOrder));
            }

            if (child.childCount > 0)
            {
                // Группа Aseprite: её имя становится частью пути, чтобы иерархия в сцене повторяла файл.
                string childPath = string.IsNullOrEmpty(groupPath) ? child.name : groupPath + "/" + child.name;
                Collect(child, childPath, layers);
            }
        }
    }

    /*
    Слоя границ камеры в файле нет — строим прямоугольник по габаритам всех слоёв.
    Без этого конфайнер остался бы со старой ссылкой и камера показывала бы пустоту за краем уровня.
    */
    private static void BuildFallbackCameraBounds(List<LocationLayerSource> layers, LocationBuildContext context)
    {
        if (context.Confiner == null)
        {
            context.Warn("Слоя границ камеры (cam_*) нет, и CinemachineConfiner2D в сцене не найден — границы не настроены");
            return;
        }

        if (!TryGetLayersBounds(layers, out Vector2 min, out Vector2 max))
        {
            context.Warn("Слоя границ камеры (cam_*) нет, и габариты уровня посчитать не удалось");
            return;
        }

        GameObject target = context.CreateObject(FallbackCameraBoundsName, context.Root);
        target.layer = context.ResolveLayer("Ignore Raycast", FallbackCameraBoundsName);

        PolygonCollider2D collider = target.AddComponent<PolygonCollider2D>();
        collider.isTrigger = true;
        collider.pathCount = 1;
        collider.SetPath(0, new[]
        {
            new Vector2(min.x, min.y),
            new Vector2(max.x, min.y),
            new Vector2(max.x, max.y),
            new Vector2(min.x, max.y)
        });

        context.Confiner.BoundingShape2D = collider;
        context.Confiner.InvalidateBoundingShapeCache();

        context.Warn($"Слоя границ камеры (cam_*) нет — построен прямоугольник по габаритам уровня и подключён к {context.Confiner.name}");
    }

    private static void FillLevelSize(List<LocationLayerSource> layers, LocationBuildConfig config, LocationBuildReport report)
    {
        report.WorldScale = config.worldScale;

        if (TryGetLayersBounds(layers, out Vector2 min, out Vector2 max))
        {
            report.LevelSize = (max - min) * config.worldScale;
        }
    }

    // Габариты в локальных единицах корня: спрайты берутся целиком, без чтения пикселей.
    private static bool TryGetLayersBounds(List<LocationLayerSource> layers, out Vector2 min, out Vector2 max)
    {
        min = new Vector2(float.MaxValue, float.MaxValue);
        max = new Vector2(float.MinValue, float.MinValue);
        bool found = false;

        foreach (LocationLayerSource layer in layers)
        {
            Sprite sprite = layer.Sprite;

            if (sprite == null)
            {
                continue;
            }

            Rect rect = sprite.textureRect.width > 0f ? sprite.textureRect : sprite.rect;
            Vector2 pivot = sprite.pivot - sprite.textureRectOffset;
            float pixelsPerUnit = sprite.pixelsPerUnit;

            Vector2 layerMin = -pivot / pixelsPerUnit;
            Vector2 layerMax = new Vector2(rect.width - pivot.x, rect.height - pivot.y) / pixelsPerUnit;

            min = Vector2.Min(min, layerMin);
            max = Vector2.Max(max, layerMax);
            found = true;
        }

        return found;
    }
}
