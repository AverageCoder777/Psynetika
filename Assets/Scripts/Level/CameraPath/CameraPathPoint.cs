using System;
using UnityEngine;

/*
Точка пути камеры: где проходит линия и насколько в этом месте камере можно отойти от неё
по вертикали.

up/down — это не сдвиг линии, а расширение коридора: при нуле камера держится ровно на линии,
при up = 8 в этом месте она может подняться на 8 юнитов выше (высокий зал, прыжок на уступ).
Между точками запас меняется плавно, поэтому коридор не ломается ступенькой.
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

    public CameraPathPoint(Vector2 position, float up = 0f, float down = 0f)
    {
        this.position = position;
        this.up = up;
        this.down = down;
    }

    public static CameraPathPoint Lerp(CameraPathPoint from, CameraPathPoint to, float t)
    {
        return new CameraPathPoint(
            Vector2.Lerp(from.position, to.position, t),
            Mathf.Lerp(from.up, to.up, t),
            Mathf.Lerp(from.down, to.down, t));
    }
}
