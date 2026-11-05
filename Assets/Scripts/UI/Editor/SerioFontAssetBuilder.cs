using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/*
Сборка TMP-ассетов для пиксельного шрифта Serio (Assets/Fonts/Serio, все шесть вариантов).

Шрифт нарисован по сетке 13 пикселей на em, поэтому атлас растеризуется без сглаживания (RASTER)
при размере 52 — ровно 4 текселя на пиксель шрифта, — а текстура фильтруется по точкам. SDF для
такого шрифта даёт скруглённые углы и мыло на мелком кегле.

Уже существующий ассет не пересоздаётся (на него ссылаются сцены по GUID) — в него только
дозапекаются недостающие символы.
*/
public static class SerioFontAssetBuilder
{
    private const string FontFolder = "Assets/Fonts/Serio";
    private const int SamplingPointSize = 52;
    private const int AtlasPadding = 2;
    private const int AtlasSize = 1024;

    [MenuItem("Psynetika/Шрифты/Собрать TMP-ассеты Serio")]
    private static void BuildAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Font", new[] { FontFolder });
        if (guids.Length == 0)
        {
            Debug.LogWarning($"В {FontFolder} нет шрифтов.");
            return;
        }

        string characters = BakedCharacters();
        foreach (string guid in guids)
        {
            string fontPath = AssetDatabase.GUIDToAssetPath(guid);
            BuildOne(AssetDatabase.LoadAssetAtPath<Font>(fontPath), fontPath, characters);
        }

        AssetDatabase.SaveAssets();
    }

    private static void BuildOne(Font font, string fontPath, string characters)
    {
        string assetPath = Path.ChangeExtension(fontPath, null) + " Pixel.asset";
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null)
        {
            existing.TryAddCharacters(characters);
            existing.atlasTexture.filterMode = FilterMode.Point;
            EditorUtility.SetDirty(existing);
            Debug.Log($"{assetPath}: символы дозапечены.", existing);
            return;
        }

        // Мультиатлас выключен: новые страницы атласа TMP создаёт с билинейной фильтрацией,
        // а на одну страницу 1024² весь набор символов помещается с запасом.
        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, SamplingPointSize, AtlasPadding,
            GlyphRenderMode.RASTER, AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, false);
        if (asset == null)
        {
            return;
        }

        asset.name = Path.GetFileNameWithoutExtension(assetPath);
        asset.TryAddCharacters(characters, out string missing);
        asset.atlasTexture.filterMode = FilterMode.Point;

        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
        AssetDatabase.AddObjectToAsset(asset.material, asset);

        Debug.Log($"{assetPath}: создан, нет в шрифте: «{missing}».", asset);
    }

    // ASCII, Latin-1, русский алфавит и типографские знаки, которые есть в Serio.
    private static string BakedCharacters()
    {
        StringBuilder sb = new();
        for (char c = ' '; c <= '~'; c++) sb.Append(c);
        for (char c = '¡'; c <= 'ÿ'; c++) sb.Append(c);
        for (char c = 'А'; c <= 'я'; c++) sb.Append(c);
        sb.Append("Ёё№–—‘’‚“”„•…‹›€™−");
        return sb.ToString();
    }
}
