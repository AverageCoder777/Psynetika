using UnityEngine;

/*
Один слой Aseprite, подготовленный к сборке.

Данные берутся не из .aseprite напрямую (классы Layer/Cell в пакете com.unity.2d.aseprite internal),
а из model prefab, который импортёр генерирует при layerImportMode = Individual Layers: там на каждый
слой создаётся GameObject с именем слоя и SpriteRenderer, а группы Aseprite дают вложенность.
*/
public class LocationLayerSource
{
    // Имя слоя в Aseprite — по нему подбирается правило.
    public string Name { get; }

    // Путь групп Aseprite до слоя ("Передний план/Земля"), пустой для слоя в корне.
    public string GroupPath { get; }

    public Sprite Sprite { get; }

    // Порядок слоя в Aseprite: импортёр кладёт его в SpriteRenderer.sortingOrder.
    public int SortingOrder { get; }

    public LocationLayerSource(string name, string groupPath, Sprite sprite, int sortingOrder)
    {
        Name = name;
        GroupPath = groupPath;
        Sprite = sprite;
        SortingOrder = sortingOrder;
    }
}
