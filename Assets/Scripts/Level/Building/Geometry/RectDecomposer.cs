using System.Collections.Generic;
using UnityEngine;

/*
Разбиение маски на непересекающиеся прямоугольники — для режима Boxes.

Алгоритм жадный: от первого свободного пикселя тянется горизонтальная полоса, затем она опускается
вниз, пока следующая строка целиком заполнена. Для пиксель-арта с прямыми стенами и платформами это
даёт единицы прямоугольников вместо тысяч пикселей; для наклонных поверхностей получится лесенка —
для них в правиле выбирается режим Outline.
*/
public static class RectDecomposer
{
    public static List<RectInt> Decompose(SpriteMask2D mask, int minArea)
    {
        List<RectInt> result = new();

        if (mask == null || mask.SolidCount == 0)
        {
            return result;
        }

        bool[] used = new bool[mask.Width * mask.Height];

        for (int y = 0; y < mask.Height; y++)
        {
            for (int x = 0; x < mask.Width; x++)
            {
                if (!IsFree(mask, used, x, y))
                {
                    continue;
                }

                int width = 0;

                while (x + width < mask.Width && IsFree(mask, used, x + width, y))
                {
                    width++;
                }

                int height = 1;

                while (y + height < mask.Height && IsRowFree(mask, used, x, y + height, width))
                {
                    height++;
                }

                for (int row = 0; row < height; row++)
                {
                    int rowStart = (y + row) * mask.Width;

                    for (int column = 0; column < width; column++)
                    {
                        used[x + column + rowStart] = true;
                    }
                }

                if (width * height >= minArea)
                {
                    result.Add(new RectInt(x, y, width, height));
                }
            }
        }

        return result;
    }

    // Габариты непрозрачных пикселей: для режима BoundingBox и для границ камеры.
    public static bool TryGetBounds(SpriteMask2D mask, out RectInt bounds)
    {
        bounds = default;

        if (mask == null || mask.SolidCount == 0)
        {
            return false;
        }

        int minX = mask.Width;
        int minY = mask.Height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < mask.Height; y++)
        {
            for (int x = 0; x < mask.Width; x++)
            {
                if (!mask[x, y])
                {
                    continue;
                }

                if (x < minX)
                {
                    minX = x;
                }

                if (x > maxX)
                {
                    maxX = x;
                }

                if (y < minY)
                {
                    minY = y;
                }

                if (y > maxY)
                {
                    maxY = y;
                }
            }
        }

        bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);

        return true;
    }

    private static bool IsFree(SpriteMask2D mask, bool[] used, int x, int y)
    {
        return mask[x, y] && !used[x + y * mask.Width];
    }

    private static bool IsRowFree(SpriteMask2D mask, bool[] used, int x, int y, int width)
    {
        for (int i = 0; i < width; i++)
        {
            if (!IsFree(mask, used, x + i, y))
            {
                return false;
            }
        }

        return true;
    }
}
