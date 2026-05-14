using UnityEditor;
using UnityEngine;

public static class AbilitySampleCreator
{
    private const string LegacySpellPath = "Assets/Resource/SpellsData/Satan/FireBullet.asset";
    private const string LegacyPrefabPath = "Assets/Prefabs/Player/Spells/FireBullet.prefab";
    private const string OutputFolder = "Assets/Resource/Abilities/Satan";
    private const string OutputProjectilePath = OutputFolder + "/FireBulletProjectile.asset";
    private const string OutputAbilityPath = OutputFolder + "/FireBulletAbility.asset";
    private const string OutputSpellBridgePath = OutputFolder + "/FireBulletSpell.asset";
    private const string OutputPrefabPath = "Assets/Prefabs/Player/Spells/FireBulletAbility.prefab";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";

    [MenuItem("Psynetika/Abilities/Create Fire Bullet Sample")]
    public static void CreateFireBulletSample()
    {
        ProjectileSpellData legacySpell = AssetDatabase.LoadAssetAtPath<ProjectileSpellData>(LegacySpellPath);
        GameObject legacyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LegacyPrefabPath);

        if (legacySpell == null || legacyPrefab == null)
        {
            Debug.LogError("[AbilitySampleCreator] Legacy Fire Bullet assets were not found.");
            return;
        }

        EnsureFolder(OutputFolder);
        GameObject runtimePrefab = CreateRuntimePrefab(legacyPrefab);
        ProjectileDefinition projectile = CreateProjectileDefinition(legacySpell, runtimePrefab);
        AbilityDefinition ability = CreateAbilityDefinition(legacySpell, projectile);
        AbilitySpellData bridge = CreateBridgeSpell(legacySpell, ability);

        MaybeAssignToPlayerPrefab(legacySpell, bridge);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(bridge);
        Debug.Log("[AbilitySampleCreator] Fire Bullet ability assets created.");
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static GameObject CreateRuntimePrefab(GameObject legacyPrefab)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(LegacyPrefabPath);
        RemoveIfExists<Projectile>(prefabRoot);
        RemoveIfExists<Firebullet>(prefabRoot);
        RemoveIfExists<ProjectileRuntime>(prefabRoot);

        GameObject runtimePrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, OutputPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        return runtimePrefab != null ? runtimePrefab : AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabPath);
    }

    private static ProjectileDefinition CreateProjectileDefinition(ProjectileSpellData legacySpell, GameObject runtimePrefab)
    {
        ProjectileDefinition projectile = LoadOrCreateAsset<ProjectileDefinition>(OutputProjectilePath);
        projectile.id = "satan.fire_bullet.projectile";
        projectile.prefab = runtimePrefab;
        projectile.speed = 10f;
        projectile.lifetime = 5f;
        projectile.spawnOffset = legacySpell.spawnOffset;

        projectile.onSpawn.Clear();
        projectile.onHit.Clear();
        projectile.onExpire.Clear();
        projectile.onHit.Add(new DamageNode
        {
            amount = 10f,
            type = DamageType.Fire
        });
        projectile.onHit.Add(new ApplyBurnNode
        {
            duration = 2f,
            tickInterval = 0.5f,
            tickDamage = 2
        });

        EditorUtility.SetDirty(projectile);
        return projectile;
    }

    private static AbilityDefinition CreateAbilityDefinition(ProjectileSpellData legacySpell, ProjectileDefinition projectile)
    {
        AbilityDefinition ability = LoadOrCreateAsset<AbilityDefinition>(OutputAbilityPath);
        ability.id = "satan.fire_bullet";
        ability.slot = AbilitySlot.Regular;
        ability.cooldown = legacySpell.spellCooldown;
        ability.animClip = legacySpell.animClip;
        ability.castDuration = legacySpell.castDuration;
        ability.castMomentNormalized = legacySpell.castMomentNormalized;
        ability.costs.Clear();
        ability.tags.Clear();
        ability.tags.Add("fire");
        ability.tags.Add("projectile");
        ability.root.Clear();
        ability.root.Add(new SpawnProjectileNode
        {
            projectile = projectile
        });

        EditorUtility.SetDirty(ability);
        return ability;
    }

    private static AbilitySpellData CreateBridgeSpell(ProjectileSpellData legacySpell, AbilityDefinition ability)
    {
        AbilitySpellData bridge = LoadOrCreateAsset<AbilitySpellData>(OutputSpellBridgePath);
        bridge.Id = legacySpell.Id;
        bridge.SpellName = legacySpell.SpellName + " (Ability)";
        bridge.SpellDescription = legacySpell.SpellDescription;
        bridge.SpellIcon = legacySpell.SpellIcon;
        bridge.Owner = legacySpell.Owner;
        bridge.Slot = legacySpell.Slot;
        bridge.spellCooldown = legacySpell.spellCooldown;
        bridge.castDuration = legacySpell.castDuration;
        bridge.castMomentNormalized = legacySpell.castMomentNormalized;
        bridge.animClip = legacySpell.animClip;
        bridge.ability = ability;

        EditorUtility.SetDirty(bridge);
        return bridge;
    }

    private static void MaybeAssignToPlayerPrefab(SpellData legacySpell, SpellData newSpell)
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab == null)
        {
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        SpellController controller = prefabRoot.GetComponent<SpellController>();
        if (controller == null)
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        SerializedObject so = new SerializedObject(controller);
        SerializedProperty satanRegular = so.FindProperty("satanRegular");
        if (satanRegular != null && satanRegular.objectReferenceValue == legacySpell)
        {
            satanRegular.objectReferenceValue = newSpell;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            Debug.Log("[AbilitySampleCreator] Player prefab updated to use Fire Bullet ability spell.");
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private static T LoadOrCreateAsset<T>(string assetPath) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, assetPath);
        return asset;
    }

    private static void RemoveIfExists<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component != null)
        {
            Object.DestroyImmediate(component, true);
        }
    }
}
