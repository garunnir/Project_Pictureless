// ============================================================
// HudLayoutPatchMenu — Dist/MCP HUD Layout Participant Patch
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class HudLayoutPatchMenu
{
    const string TimePath = "Assets/Dist/Visual/Prefabs/UIComponents/Time/Grp_TimeDisplay.prefab";
    const string TimeScalePath = "Assets/Dist/Visual/Prefabs/UIComponents/Time/Hud_TimeScale.prefab";
    const string MessageLogPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/MessageLog/Hud_MessageLog.prefab";
    const string SummaryPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/PlayerStatus/Grp_PlayerStatusSummary.prefab";

    [MenuItem(DistMcpMenus.HudLayoutPatchParticipants)]
    static void PatchAll()
    {
        PatchPrefab(
            TimePath,
            HudLayoutIds.TimeDisplay,
            TimeUIFactory.MinPanelSize,
            TimeUIFactory.MaxPanelSize,
            ensureResizeChrome: false);
        PatchPrefab(
            MessageLogPath,
            HudLayoutIds.MessageLog,
            new Vector2(200f, 80f),
            new Vector2(800f, 480f),
            ensureResizeChrome: true);
        PatchPrefab(
            SummaryPath,
            HudLayoutIds.PlayerStatusSummary,
            PlayerStatusUIFactory.SummaryPanelSize,
            new Vector2(480f, 120f),
            ensureResizeChrome: true);
        PatchPrefab(
            TimeScalePath,
            TimeScaleHudLayout.ParticipantId,
            TimeScaleHudLayout.PanelSize,
            TimeUIFactory.MaxPanelSize,
            ensureResizeChrome: false);

        AssetDatabase.SaveAssets();
        Debug.Log("[HudLayoutPatchMenu] HudLayoutParticipant patched on HUD prefabs.");
    }

    static void PatchPrefab(
        string path,
        string participantId,
        Vector2 minSize,
        Vector2 maxSize,
        bool ensureResizeChrome)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            Debug.LogWarning($"[HudLayoutPatchMenu] Skipped missing prefab: {path}");
            return;
        }

        try
        {
            HudLayoutPrefabPatch.Apply(
                root,
                participantId,
                minSize,
                maxSize,
                ensureOverlayWindow: true,
                ensureResizeChrome: ensureResizeChrome,
                resizeEdgeThickness: TimeUIFactory.ResizeEdgeThickness);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
