using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

/*
Состояние одной сборки локации: куда складывать объекты, какими настройками резать маски,
куда писать отчёт. Через контекст действия слоёв получают всё, что им нужно, и не знают
ни про редактор, ни про окно утилиты.

Маски и прочитанные текстуры кешируются на время сборки: спрайты уровня лежат в одном атласе,
и без кеша он копировался бы заново на каждый слой.
*/
public class LocationBuildContext
{
    private readonly SpriteMask2D.TextureCache textureCache = new();
    private readonly Dictionary<Sprite, SpriteMask2D> maskCache = new();
    private readonly Dictionary<string, Transform> groups = new();
    private readonly Action<GameObject> objectCreated;

    public Transform Root { get; }
    public LocationBuildConfig Config { get; }
    public LocationBuildReport Report { get; }

    // Конфайнер активной камеры: сюда CameraBoundsAction подставит сгенерированные границы.
    public CinemachineConfiner2D Confiner { get; }

    // Строка отчёта обрабатываемого сейчас слоя — действия дописывают в неё, что создали.
    public LocationBuildEntry CurrentEntry { get; set; }

    public LocationBuildContext(
        Transform root,
        LocationBuildConfig config,
        LocationBuildReport report,
        CinemachineConfiner2D confiner,
        Action<GameObject> objectCreated)
    {
        Root = root;
        Config = config;
        Report = report;
        Confiner = confiner;
        this.objectCreated = objectCreated;
    }

    public SpriteMask2D GetMask(Sprite sprite)
    {
        if (sprite == null)
        {
            return null;
        }

        if (maskCache.TryGetValue(sprite, out SpriteMask2D cached))
        {
            return cached;
        }

        SpriteMask2D mask = SpriteMask2D.FromSprite(sprite, Config.alphaThreshold, textureCache);
        maskCache[sprite] = mask;

        return mask;
    }

    // Объект слоя под корнем сборки, с воссозданной иерархией групп Aseprite.
    public GameObject CreateLayerObject(LocationLayerSource layer, float zOffset)
    {
        GameObject created = CreateObject(layer.Name, ResolveGroup(layer.GroupPath));
        created.transform.localPosition = new Vector3(0f, 0f, zOffset);

        return created;
    }

    // Единственная точка создания объектов сборки: через неё редактор узнаёт, что регистрировать в Undo.
    public GameObject CreateObject(string name, Transform parent)
    {
        GameObject created = new(name);
        created.transform.SetParent(parent != null ? parent : Root, false);

        objectCreated?.Invoke(created);

        return created;
    }

    public void Note(string text)
    {
        if (CurrentEntry == null)
        {
            return;
        }

        CurrentEntry.result = string.IsNullOrEmpty(CurrentEntry.result) ? text : CurrentEntry.result + ", " + text;
    }

    public void Warn(string message)
    {
        Report.Warn(message);
    }

    // Слой физики по имени: неизвестное имя не должно молча уводить объект в Default.
    public int ResolveLayer(string layerName, string layerLabel)
    {
        if (string.IsNullOrWhiteSpace(layerName))
        {
            return 0;
        }

        int index = LayerMask.NameToLayer(layerName);

        if (index >= 0)
        {
            return index;
        }

        Warn($"Слой «{layerLabel}»: в проекте нет слоя физики «{layerName}», объект остался на Default");

        return 0;
    }

    // Тег по имени: несуществующий тег бросает исключение при присваивании, поэтому пробуем аккуратно.
    public void ApplyTag(GameObject target, string tagName, string layerLabel)
    {
        if (string.IsNullOrWhiteSpace(tagName) || tagName == "Untagged")
        {
            return;
        }

        try
        {
            target.tag = tagName;
        }
        catch (UnityException)
        {
            Warn($"Слой «{layerLabel}»: в проекте нет тега «{tagName}», тег не назначен");
        }
    }

    private Transform ResolveGroup(string groupPath)
    {
        if (string.IsNullOrEmpty(groupPath))
        {
            return Root;
        }

        if (groups.TryGetValue(groupPath, out Transform cached))
        {
            return cached;
        }

        Transform parent = Root;
        string accumulated = string.Empty;

        foreach (string part in groupPath.Split('/'))
        {
            accumulated = string.IsNullOrEmpty(accumulated) ? part : accumulated + "/" + part;

            if (!groups.TryGetValue(accumulated, out Transform group))
            {
                group = CreateObject(part, parent).transform;
                groups[accumulated] = group;
            }

            parent = group;
        }

        return parent;
    }
}
