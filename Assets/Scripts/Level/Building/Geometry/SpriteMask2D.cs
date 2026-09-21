using System.Collections.Generic;
using UnityEngine;

/*
Пиксельная маска одного спрайта: что считать «телом» слоя при построении коллайдера.

Зачем отдельный класс: спрайты уровня лежат в общем атласе (generateSpriteAtlas у AsepriteImporter),
который импортируется нечитаемым (isReadable: 0). Читать его через GetPixels32() напрямую нельзя,
поэтому текстура один раз копируется через RenderTexture и кешируется на всю сборку — иначе
копирование повторялось бы на каждый слой.

Система координат: пиксель (0,0) — левый нижний угол sprite.rect. Перевод в локальные единицы
объекта слоя делает ToLocal(): именно такую раскладку даёт AsepriteImporter при pivotSpace = Canvas,
когда у всех слоёв localPosition = 0, а положение на холсте закодировано в пивоте спрайта.
*/
public class SpriteMask2D
{
    private readonly bool[] pixels;
    private readonly Vector2 pivot;
    private readonly float pixelsPerUnit;

    public int Width { get; }
    public int Height { get; }

    // Количество непрозрачных пикселей: по нему видно, что слой пустой.
    public int SolidCount { get; }

    private SpriteMask2D(bool[] pixels, int width, int height, int solidCount, Vector2 pivot, float pixelsPerUnit)
    {
        this.pixels = pixels;
        this.pivot = pivot;
        this.pixelsPerUnit = pixelsPerUnit;
        Width = width;
        Height = height;
        SolidCount = solidCount;
    }

    public bool this[int x, int y] => x >= 0 && y >= 0 && x < Width && y < Height && pixels[x + y * Width];

    // Перевод точки сетки пикселей в локальные координаты объекта слоя.
    // Координата может быть дробной и выходить за границы маски: контур идёт по рёбрам пикселей.
    public Vector2 ToLocal(float x, float y)
    {
        return new Vector2((x - pivot.x) / pixelsPerUnit, (y - pivot.y) / pixelsPerUnit);
    }

    public static SpriteMask2D FromSprite(Sprite sprite, float alphaThreshold, TextureCache cache)
    {
        if (sprite == null || sprite.texture == null)
        {
            return null;
        }

        Color32[] texels = cache.GetPixels(sprite.texture, out int texWidth, out int texHeight);

        if (texels == null)
        {
            return null;
        }

        Rect rect = sprite.textureRect.width > 0f ? sprite.textureRect : sprite.rect;
        int originX = Mathf.RoundToInt(rect.x);
        int originY = Mathf.RoundToInt(rect.y);
        int width = Mathf.RoundToInt(rect.width);
        int height = Mathf.RoundToInt(rect.height);

        if (width <= 0 || height <= 0)
        {
            return null;
        }

        byte threshold = (byte)Mathf.Clamp(Mathf.RoundToInt(alphaThreshold * 255f), 1, 255);
        bool[] mask = new bool[width * height];
        int solid = 0;

        for (int y = 0; y < height; y++)
        {
            int texY = originY + y;

            if (texY < 0 || texY >= texHeight)
            {
                continue;
            }

            int rowStart = texY * texWidth;

            for (int x = 0; x < width; x++)
            {
                int texX = originX + x;

                if (texX < 0 || texX >= texWidth)
                {
                    continue;
                }

                if (texels[rowStart + texX].a >= threshold)
                {
                    mask[x + y * width] = true;
                    solid++;
                }
            }
        }

        // Пивот спрайта задан относительно его rect, а при packing'е с обрезкой прозрачных краёв
        // textureRect смещён относительно исходного rect — компенсируем через textureRectOffset.
        Vector2 spritePivot = sprite.pivot - sprite.textureRectOffset;

        return new SpriteMask2D(mask, width, height, solid, spritePivot, sprite.pixelsPerUnit);
    }

    /*
    Кеш прочитанных текстур на одну сборку локации.
    Нечитаемая текстура копируется через временный RenderTexture: это единственный способ
    достать пиксели атласа, не трогая настройки импорта ассета.
    */
    public class TextureCache
    {
        private readonly Dictionary<Texture2D, Color32[]> cachedPixels = new();
        private readonly Dictionary<Texture2D, Vector2Int> cachedSizes = new();

        public Color32[] GetPixels(Texture2D texture, out int width, out int height)
        {
            if (texture == null)
            {
                width = 0;
                height = 0;
                return null;
            }

            if (cachedPixels.TryGetValue(texture, out Color32[] cached))
            {
                Vector2Int size = cachedSizes[texture];
                width = size.x;
                height = size.y;
                return cached;
            }

            width = texture.width;
            height = texture.height;
            Color32[] pixels = texture.isReadable ? texture.GetPixels32() : ReadUnreadable(texture);

            cachedPixels[texture] = pixels;
            cachedSizes[texture] = new Vector2Int(width, height);

            return pixels;
        }

        private static Color32[] ReadUnreadable(Texture2D texture)
        {
            RenderTexture temporary = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);

            RenderTexture previous = RenderTexture.active;
            Texture2D readable = null;

            try
            {
                Graphics.Blit(texture, temporary);
                RenderTexture.active = temporary;

                readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
                readable.Apply();

                return readable.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);

                if (readable != null)
                {
                    Object.DestroyImmediate(readable);
                }
            }
        }
    }
}
