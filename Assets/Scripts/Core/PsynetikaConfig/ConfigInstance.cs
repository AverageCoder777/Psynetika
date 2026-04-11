using UnityEngine;

public static class ConfigInstance
{
    private const string DefaultResourcePath = "PsynetikaConfig";
    private static PsynetikaConfig config;

    public static PsynetikaConfig Value
    {
        get
        {
            if (config != null)
            {
                return config;
            }

            config = Resources.Load<PsynetikaConfig>(DefaultResourcePath);

#if UNITY_EDITOR
            if (config == null)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("t:PsynetikaConfig");
                if (guids.Length > 0)
                {
                    var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    config = UnityEditor.AssetDatabase.LoadAssetAtPath<PsynetikaConfig>(assetPath);
                }
            }
#endif

            if (config == null)
            {
                Debug.LogError(
                    "PsynetikaConfig not found. Create one via Create/Psynetika/Config and place it in Resources/PsynetikaConfig.asset, or initialize with ConfigInstance.Set(...).");
            }

            return config;
        }
    }

    public static void Set(PsynetikaConfig instance)
    {
        if (instance == null)
        {
            Debug.LogError("ConfigInstance.Set called with null.");
            return;
        }

        config = instance;
    }
}
