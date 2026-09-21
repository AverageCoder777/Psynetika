using System;
using UnityEngine;

/*
Что сделать со слоем Aseprite при сборке локации.

Новый вид разметки = новый наследник с [AddTypeMenu] и без единой правки билдера: класс сразу
появится в выпадающем списке правил LocationBuildConfig (SubclassSelector). Тот же приём, что у
EnemyAttackModule в конфиге врага и у AbilityNode в способностях.
*/
[Serializable]
public abstract class LocationLayerAction
{
    [Tooltip("Рисовать спрайт слоя. У слоёв-разметки (коллизия, границы камеры) выключено")]
    public bool renderSprite;

    [Tooltip("Sorting layer спрайта. Работает, только если спрайт рисуется")]
    [SortingLayerName] public string sortingLayer = "Default";

    [Tooltip("Сдвиг порядка отрисовки относительно порядка слоя в Aseprite")]
    public int sortingOrderOffset;

    [Tooltip("Сдвиг объекта по Z: разводит слои по глубине, на 2D-физику не влияет")]
    public float zOffset;

    // Короткое описание для таблицы предпросмотра в окне утилиты.
    public abstract string Describe();

    // Подписи для описания: пустая строка в разметке значит «как в Unity по умолчанию».
    protected static string LayerLabel(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Default" : value;
    }

    protected static string TagLabel(string value)
    {
        return string.IsNullOrWhiteSpace(value) || value == "Untagged" ? "нет" : value;
    }

    public abstract void Apply(LocationLayerSource layer, LocationBuildContext ctx);

    // Объект слоя под корнем сборки. Спрайт вешается, только если правило этого просит.
    protected GameObject CreateObject(LocationLayerSource layer, LocationBuildContext ctx)
    {
        GameObject created = ctx.CreateLayerObject(layer, zOffset);

        if (!renderSprite)
        {
            return created;
        }

        SpriteRenderer renderer = created.AddComponent<SpriteRenderer>();
        renderer.sprite = layer.Sprite;
        renderer.sortingOrder = layer.SortingOrder + sortingOrderOffset;

        if (!string.IsNullOrWhiteSpace(sortingLayer))
        {
            if (SortingLayer.NameToID(sortingLayer) != 0 || sortingLayer == "Default")
            {
                renderer.sortingLayerName = sortingLayer;
            }
            else
            {
                ctx.Warn($"Слой «{layer.Name}»: в проекте нет sorting layer «{sortingLayer}», остался Default");
            }
        }

        return created;
    }

    // Общая часть всех коллизионных действий: построить фигуру и настроить объект.
    protected Collider2D BuildCollider(
        GameObject target,
        LocationLayerSource layer,
        LocationBuildContext ctx,
        ColliderShapeMode shape,
        bool isTrigger,
        float boundsPadding)
    {
        SpriteMask2D mask = ctx.GetMask(layer.Sprite);

        if (mask == null || mask.SolidCount == 0)
        {
            ctx.Warn($"Слой «{layer.Name}»: непрозрачных пикселей нет, коллайдер не построен");
            ctx.Note("пусто");

            return null;
        }

        Collider2D collider = LocationColliderFactory.Build(
            target,
            mask,
            shape,
            ctx.Config,
            isTrigger,
            boundsPadding,
            out int pieces);

        if (collider == null)
        {
            ctx.Warn($"Слой «{layer.Name}»: фигура оказалась мельче порогов конфига, коллайдер не построен");
            ctx.Note("отсеяно порогами");

            return null;
        }

        ctx.Note(shape == ColliderShapeMode.Boxes
            ? $"{pieces} прямоугольников"
            : $"{pieces} контуров");

        return collider;
    }
}
