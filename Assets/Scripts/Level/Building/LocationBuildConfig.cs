using System.Collections.Generic;
using UnityEngine;

/*
Все настройки сборки локации в одном ассете: пороги резки пикселей и таблица правил
«маска имени слоя → действие». Новая конвенция именования = правка данных, а не кода.

Пороги стоит понимать так:
  alphaThreshold    — с какой непрозрачности пиксель считается телом слоя;
  simplifyTolerance — насколько сильно сглаживать ступеньки пиксель-арта, в пикселях;
  minContourArea    — контуры мельче этой площади выбрасываются (одиночные пиксели, пыль);
  minRectArea       — то же для режима прямоугольников.
*/
[CreateAssetMenu(menuName = "Psynetika/Location Build Config", fileName = "LocationBuildConfig")]
public class LocationBuildConfig : ScriptableObject
{
    [Header("Масштаб")]
    [Tooltip("Множитель масштаба корня локации. Арт уровня в Level 1 стоит с масштабом 6: " +
             "при PPU 100 это ~16.7 пикселя арта на юнит, персонаж ростом 2.08 юнита занимает ~35 пикселей")]
    [Min(0.0001f)] public float worldScale = 6f;

    [Header("Маска")]
    [Tooltip("Минимальная непрозрачность пикселя, чтобы он попал в коллайдер")]
    [Range(0.01f, 1f)] public float alphaThreshold = 0.5f;

    [Header("Контур")]
    [Tooltip("Порог упрощения в пикселях: 0 — точный контур по ступенькам, 1.5–3 — гладко и дёшево")]
    [Min(0f)] public float simplifyTolerance = 1.5f;

    [Tooltip("Контуры короче этого числа точек выбрасываются")]
    [Min(3)] public int minPathPoints = 3;

    [Tooltip("Контуры с площадью меньше этой (в пикселях) выбрасываются")]
    [Min(0f)] public float minContourArea = 8f;

    [Header("Прямоугольники")]
    [Tooltip("Прямоугольники площадью меньше этой (в пикселях) выбрасываются")]
    [Min(1)] public int minRectArea = 4;

    [Header("Правила слоёв")]
    [Tooltip("Берётся первое правило, чья маска совпала с именем слоя")]
    public List<LocationLayerRule> rules = new();

    [Tooltip("Что делать со слоем, которому не подошло ни одно правило")]
    [SerializeReference, SubclassSelector]
    public LocationLayerAction defaultAction = new DecorationAction();

    public LocationLayerAction ResolveAction(string layerName, out string ruleLabel, out bool matched)
    {
        foreach (LocationLayerRule rule in rules)
        {
            if (rule == null || rule.action == null || !rule.Matches(layerName))
            {
                continue;
            }

            ruleLabel = rule.namePattern;
            matched = true;

            return rule.action;
        }

        ruleLabel = "по умолчанию";
        matched = false;

        return defaultAction;
    }

    // Стартовый набор правил: его создаёт кнопка в окне утилиты, дальше его правят руками.
    public static LocationBuildConfig CreateDefault()
    {
        LocationBuildConfig config = CreateInstance<LocationBuildConfig>();

        config.rules = new List<LocationLayerRule>
        {
            // Фон: глубина по суффиксу в имени, первое совпадение выигрывает. Небо приклеено к камере.
            // Параллакс только по горизонтали: по вертикали слои стоят как нарисованы.
            Parallax("bg_sky*", 1f),
            Parallax("bg_*far*", 0.8f),
            Parallax("bg_*mid*", 0.5f),
            Parallax("bg_*near*", 0.25f),
            Parallax("bg_*", 0.5f),
            new() { namePattern = "art_*", action = new DecorationAction { renderSprite = true } },
            // Передний план обгоняет мир. art_fg_* сюда не попадает: маска сверяется с начала имени.
            Parallax("fg_*", -0.3f),
            new()
            {
                namePattern = "col_*",
                action = new SolidColliderAction
                {
                    shape = ColliderShapeMode.Boxes,
                    physicsLayer = "Floor",
                    tag = "Floor"
                }
            },
            new()
            {
                namePattern = "wall_*",
                action = new SolidColliderAction
                {
                    shape = ColliderShapeMode.Boxes,
                    physicsLayer = "Walls",
                    tag = "Wall"
                }
            },
            new() { namePattern = "plat_*", action = new OneWayPlatformAction() },
            new() { namePattern = "cam_*", action = new CameraBoundsAction() },
            new() { namePattern = "dead_*", action = new DeadZoneAction() },
            new() { namePattern = "skip_*", action = new IgnoreLayerAction() }
        };

        config.defaultAction = new DecorationAction { renderSprite = true };

        return config;
    }

    private static LocationLayerRule Parallax(string namePattern, float factor)
    {
        return new LocationLayerRule
        {
            namePattern = namePattern,
            action = new ParallaxAction { horizontal = factor, vertical = 0f }
        };
    }
}
