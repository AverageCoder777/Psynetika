using System.IO;
using UnityEditor;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

/*
Редакторские операции вокруг .aseprite-ассета: понять, как он импортирован, и починить импорт.

Разметка слоями работает только при layerImportMode = Individual Layers — иначе AsepriteImporter
сплющивает все слои в один спрайт, и разбирать нечего. Настройки правятся через публичное API
импортёра, .meta руками не трогаем.
*/
public static class AsepriteImportFixer
{
    private const string DefaultConfigFolder = "Assets/Resource/Level";

    public static AsepriteImporter GetImporter(Object asset)
    {
        if (asset == null)
        {
            return null;
        }

        string path = AssetDatabase.GetAssetPath(asset);

        return string.IsNullOrEmpty(path) ? null : AssetImporter.GetAtPath(path) as AsepriteImporter;
    }

    public static bool IsAsepriteAsset(Object asset)
    {
        return GetImporter(asset) != null;
    }

    public static bool IsMergedIntoSingleLayer(Object asset)
    {
        AsepriteImporter importer = GetImporter(asset);

        return importer != null && importer.layerImportMode == LayerImportModes.MergeFrame;
    }

    public static bool SwitchToIndividualLayers(Object asset)
    {
        AsepriteImporter importer = GetImporter(asset);

        if (importer == null)
        {
            return false;
        }

        importer.layerImportMode = LayerImportModes.IndividualLayers;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        return true;
    }

    // Конфиг по умолчанию рядом с остальными ассетами данных проекта.
    public static LocationBuildConfig CreateDefaultConfig()
    {
        if (!Directory.Exists(DefaultConfigFolder))
        {
            Directory.CreateDirectory(DefaultConfigFolder);
            AssetDatabase.Refresh();
        }

        LocationBuildConfig config = LocationBuildConfig.CreateDefault();
        string path = AssetDatabase.GenerateUniqueAssetPath(DefaultConfigFolder + "/LocationBuildConfig.asset");

        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();

        return config;
    }
}
