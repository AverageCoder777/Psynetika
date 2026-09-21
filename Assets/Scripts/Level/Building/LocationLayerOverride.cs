using System;
using System.Collections.Generic;
using UnityEngine;

/*
Ручное назначение роли слою: «вот этот слой — земля», без переименования слоя в Aseprite.

Зачем в дополнение к правилам конфига: маски имён (col_*, plat_*) хороши, когда художник уже
держит конвенцию, но в существующих файлах слои называются как попало, и переименовывать их
задним числом дороже, чем один раз отметить роль в окне утилиты.

Назначения живут в маркере локации, то есть в самой сцене: у всех, кто откроет сцену, разметка
одинаковая. Приоритет выше правил конфига — совпавшее назначение отменяет любое правило.
*/
[Serializable]
public class LocationLayerOverride
{
    [Tooltip("Имя слоя Aseprite, которому назначена роль")]
    public string layer;

    [SerializeReference, SubclassSelector]
    public LocationLayerAction action;

    public static LocationLayerAction Find(List<LocationLayerOverride> overrides, string layerName)
    {
        if (overrides == null)
        {
            return null;
        }

        foreach (LocationLayerOverride item in overrides)
        {
            if (item != null && item.action != null && item.layer == layerName)
            {
                return item.action;
            }
        }

        return null;
    }

    /*
    Копия списка: окно утилиты правит свой экземпляр, маркер сцены хранит свой.
    Без копии правка в окне молча меняла бы сцену мимо Undo и мимо пометки «сцена изменена».
    */
    public static List<LocationLayerOverride> Clone(List<LocationLayerOverride> source)
    {
        List<LocationLayerOverride> copy = new();

        if (source == null)
        {
            return copy;
        }

        foreach (LocationLayerOverride item in source)
        {
            if (item == null || string.IsNullOrEmpty(item.layer))
            {
                continue;
            }

            copy.Add(new LocationLayerOverride
            {
                layer = item.layer,
                action = CloneAction(item.action)
            });
        }

        return copy;
    }

    // Поле [SerializeReference] нельзя копировать присваиванием: обе копии смотрели бы на один объект.
    // JsonUtility знает реальный тип действия и переносит ссылки на ассеты (PhysicsMaterial2D) как есть.
    public static LocationLayerAction CloneAction(LocationLayerAction action)
    {
        if (action == null)
        {
            return null;
        }

        return (LocationLayerAction)JsonUtility.FromJson(JsonUtility.ToJson(action), action.GetType());
    }
}
