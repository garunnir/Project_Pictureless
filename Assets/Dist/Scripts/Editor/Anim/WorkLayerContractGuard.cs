#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Work 클립 카탈로그 변경 시 컨트롤러 Work Layer 상태를 자동 동기화한다.
/// </summary>
public sealed class WorkLayerCatalogPostprocessor : AssetPostprocessor
{
    static readonly string[] CatalogSuffixes =
    {
        "/VaultClipCatalog.asset",
        "/FarmWorkClipCatalog.asset",
        "/FishWorkClipCatalog.asset",
    };

    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (!ShouldRefresh(importedAssets) && !ShouldRefresh(movedAssets))
            return;

        ArmOverlayAnimatorBuilder.EnsureDefaultControllerWorkLayer();
    }

    static bool ShouldRefresh(string[] paths)
    {
        if (paths == null)
            return false;

        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i];
            if (string.IsNullOrEmpty(path))
                continue;

            for (int s = 0; s < CatalogSuffixes.Length; s++)
            {
                if (path.EndsWith(CatalogSuffixes[s]))
                    return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Play 진입 전 기본 컨트롤러 Work Layer 계약 검사.
/// </summary>
[InitializeOnLoad]
static class WorkLayerPlayModeContractGuard
{
    static WorkLayerPlayModeContractGuard() =>
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

    static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.ExitingEditMode)
            return;

        ValidateDefaultController();
    }

    static void ValidateDefaultController()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            CharacterWorkLayerAnim.DefaultControllerPath);
        if (controller == null)
        {
            Debug.LogError(
                "[WorkLayerContract] CharacterAnimController missing at " +
                CharacterWorkLayerAnim.DefaultControllerPath);
            return;
        }

        int layerIndex = -1;
        AnimatorControllerLayer[] layers = controller.layers;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].name == CharacterWorkLayerAnim.LayerName)
            {
                layerIndex = i;
                break;
            }
        }

        if (layerIndex < 0)
        {
            Debug.LogError(
                "[WorkLayerContract] Default controller has no Work Layer. " +
                "Run Dist/MCP/Rebuild Arm Overlay Animator.");
            return;
        }

        if (!layers[layerIndex].iKPass)
        {
            Debug.LogWarning(
                "[WorkLayerContract] Work Layer IK Pass is off (vault Mantle IK needs it). " +
                "Run Dist/MCP/Ensure Work Layer.");
        }

        HashSet<string> stateNames = CollectStateNames(layers[layerIndex].stateMachine);
        List<string> missing = new();
        AppendMissingClipStates(
            AssetDatabase.LoadAssetAtPath<VaultClipCatalog>(VaultClipCatalog.DefaultAssetPath),
            stateNames,
            missing);
        AppendMissingClipStates(
            AssetDatabase.LoadAssetAtPath<FarmWorkClipCatalog>(FarmWorkClipCatalog.DefaultAssetPath),
            stateNames,
            missing);
        AppendMissingClipStates(
            AssetDatabase.LoadAssetAtPath<FishWorkClipCatalog>(FishWorkClipCatalog.DefaultAssetPath),
            stateNames,
            missing);

        if (missing.Count == 0)
            return;

        Debug.LogError(
            "[WorkLayerContract] Work Layer missing states for catalog clips: " +
            string.Join(", ", missing) +
            ". Run Dist/MCP/Ensure Work Layer (Vault/Farm/Fish).");
    }

    static HashSet<string> CollectStateNames(AnimatorStateMachine sm)
    {
        var names = new HashSet<string>();
        ChildAnimatorState[] states = sm.states;
        for (int i = 0; i < states.Length; i++)
            names.Add(states[i].state.name);
        return names;
    }

    static void AppendMissingClipStates(
        ScriptableObject catalog,
        HashSet<string> stateNames,
        List<string> missing)
    {
        if (catalog == null)
            return;

        SerializedObject so = new SerializedObject(catalog);
        SerializedProperty prop = so.GetIterator();
        while (prop.NextVisible(true))
        {
            if (prop.propertyType != SerializedPropertyType.ObjectReference)
                continue;
            if (prop.objectReferenceValue is not AnimationClip clip || clip == null)
                continue;
            if (stateNames.Contains(clip.name))
                continue;
            if (!missing.Contains(clip.name))
                missing.Add(clip.name);
        }
    }
}
#endif
