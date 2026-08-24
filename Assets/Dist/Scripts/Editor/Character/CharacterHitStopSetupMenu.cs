// ============================================================
// CharacterHitStopSetupMenu — Dist/MCP 히트스톱 SO·프리팹 Ensure
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CharacterHitStopSetupMenu
{
    const string NpcSamplePath = "Assets/Dist/Visual/Prefabs/3D/NpcSample.prefab";
    const string SettingsFolder = "Assets/Dist/SOData/Combat/Fallbacks";
    const string SettingsPath = CharacterHitStop.DefaultSettingsPath;

    [MenuItem(DistMcpMenus.CharacterEnsureHitStop)]
    public static void EnsureCombatHitStop()
    {
        CombatHitStopSettings settings = EnsureSettingsAsset();
        int prefabAdded = PatchNpcSamplePrefab(settings);
        int sceneAdded = PatchOpenSceneHosts(settings);
        if (!Application.isPlaying)
            EditorSceneManager.SaveOpenScenes();
        Debug.Log(
            $"[CharacterHitStopSetupMenu] Settings {SettingsPath}. " +
            $"Prefab added={prefabAdded}, scene added={sceneAdded}.",
            settings);
    }

    static CombatHitStopSettings EnsureSettingsAsset()
    {
        CombatHitStopSettings settings =
            AssetDatabase.LoadAssetAtPath<CombatHitStopSettings>(SettingsPath);
        if (settings != null)
            return settings;

        if (!AssetDatabase.IsValidFolder(SettingsFolder))
        {
            Debug.LogError($"[CharacterHitStopSetupMenu] Folder missing: {SettingsFolder}");
            return null;
        }

        settings = ScriptableObject.CreateInstance<CombatHitStopSettings>();
        AssetDatabase.CreateAsset(settings, SettingsPath);
        AssetDatabase.SaveAssets();
        return settings;
    }

    static int PatchNpcSamplePrefab(CombatHitStopSettings settings)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(NpcSamplePath);
        if (root == null)
        {
            Debug.LogError($"[CharacterHitStopSetupMenu] Failed to load: {NpcSamplePath}");
            return 0;
        }

        try
        {
            int added = EnsureHitStopOn(root, settings, recordUndo: false) ? 1 : 0;
            PrefabUtility.SaveAsPrefabAsset(root, NpcSamplePath);
            return added;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static int PatchOpenSceneHosts(CombatHitStopSettings settings)
    {
        int added = 0;
        CharacterHitReact[] hosts = Object.FindObjectsByType<CharacterHitReact>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < hosts.Length; i++)
        {
            CharacterHitReact host = hosts[i];
            if (host == null)
                continue;
            if (EnsureHitStopOn(host.gameObject, settings, recordUndo: true))
                added++;
        }

        return added;
    }

    static bool EnsureHitStopOn(
        GameObject go,
        CombatHitStopSettings settings,
        bool recordUndo)
    {
        CharacterHitStop hitStop = go.GetComponent<CharacterHitStop>();
        bool added = false;
        if (hitStop == null)
        {
            hitStop = recordUndo
                ? Undo.AddComponent<CharacterHitStop>(go)
                : go.AddComponent<CharacterHitStop>();
            added = true;
        }

        SerializedObject so = new(hitStop);
        SerializedProperty settingsProp = so.FindProperty("_settings");
        if (settingsProp != null &&
            settingsProp.objectReferenceValue != settings)
        {
            settingsProp.objectReferenceValue = settings;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(go);
        return added;
    }
}
#endif
