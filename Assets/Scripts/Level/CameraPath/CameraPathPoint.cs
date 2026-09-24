using System;
using UnityEngine;

/*
Точка пути камеры: где проходит линия и насколько в этом месте камере можно отойти от неё
по вертикали.

up/down — это не сдвиг линии, а расширение коридора: при нуле камера держится ровно на линии,
при up = 8 в этом месте она может подняться на 8 юнитов выше (высокий зал, прыжок на уступ).
Между точками запас меняется плавно, поэтому коридор не ломается ступенькой.

straight — режим отрезка, который начинается в этой точке и идёт к следующей: гибкий (изгибается
сглаживанием в кривую) или строго прямой. У последней точки незамкнутого пути флаг ни на что не влияет.

zoom — во сколько раз обзор камеры в этой точке больше обычного: 1 — как настроено у камеры,
1.5 — видно в полтора раза больше (отдаление), 0.7 — ближе. Между точками зум меняется плавно,
поэтому «участок с зумом» — это просто точки с одинаковым зумом на его концах. 0 в данных значит 1:
так читаются точки, сохранённые до появления поля (Unity заполняет новое поле структуры нулём).
*/
[Serializable]
public struct CameraPathPoint
{
    [Tooltip("Положение точки в локальных координатах пути")]
    public Vector2 position;

    [Tooltip("Насколько камере можно подняться выше линии в этой точке, в юнитах")]
    [Min(0f)] public float up;

    [Tooltip("Насколько камере можно опуститься ниже линии в этой точке, в юнитах")]
    [Min(0f)] public float down;

    [Tooltip("Отрезок от этой точки до следующей строго прямой: сглаживание его не изгибает")]
    public bool straight;

    [Tooltip("Зум камеры в этой точке: 1 — обычный обзор, 1.5 — видно в полтора раза больше (отдаление), " +
             "0.7 — ближе. Между точками меняется плавно, коридор расширяется вместе с обзором")]
    [Min(0f)] public float zoom;

    public CameraPathPoint(Vector2 position, float up = 0f, float down = 0f, bool straight = false, float zoom = 1f)
    {
        this.position = position;
        this.up = up;
        this.down = down;
        this.straight = straight;
        this.zoom = zoom;
    }

    // Зум с учётом старых данных: 0 — поле ещё не заполнялось, значит обычный обзор.
    public float Zoom => zoom > 0f ? zoom : 1f;

    public static CameraPathPoint Lerp(CameraPathPoint from, CameraPathPoint to, float t)
    {
        return new CameraPathPoint(
            Vector2.Lerp(from.position, to.position, t),
            Mathf.Lerp(from.up, to.up, t),
            Mathf.Lerp(from.down, to.down, t),
            false,
            Mathf.Lerp(from.Zoom, to.Zoom, t));
    }
}
