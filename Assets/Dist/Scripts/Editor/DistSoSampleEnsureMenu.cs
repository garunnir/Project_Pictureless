// ============================================================
// DistSoSampleEnsureMenu — Dist/MCP singleton SO 샘플 에셋 일괄 Ensure
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using IsoTilemap;
using UnityEditor;
using UnityEngine;

static class DistSoSampleEnsureMenu
{
    [MenuItem(DistMcpMenus.EnsureSampleScriptableObjects)]
    static void EnsureAllSampleScriptableObjects()
    {
        var created = new List<string>();
        var existing = new List<string>();

        EnsureAsset<CombatHitStopSettings>(CombatHitStopSettings.DefaultAssetPath, created, existing);
        EnsureAsset<WorldClockSettings>(WorldClockSettings.DefaultAssetPath, created, existing);
        EnsureAsset<MoodSettings>(MoodSettings.DefaultAssetPath, created, existing);
        var moodSettings = AssetDatabase.LoadAssetAtPath<MoodSettings>(MoodSettings.DefaultAssetPath);
        if (moodSettings != null)
        {
            moodSettings.EnsureCatalogRows();
            EditorUtility.SetDirty(moodSettings);
        }
        EnsureAsset<PlayerNeedsSettings>(PlayerNeedsSettings.DefaultAssetPath, created, existing);
        EnsureAsset<PlayerStatusMoodIconCatalog>(
            PlayerStatusMoodIconCatalog.DefaultAssetPath,
            created,
            existing);
        EnsureAsset<FarmWorkClipCatalog>(FarmWorkClipCatalog.DefaultAssetPath, created, existing);
        EnsureAsset<FishWorkClipCatalog>(FishWorkClipCatalog.DefaultAssetPath, created, existing);
        EnsureAsset<VaultClipCatalog>(VaultClipCatalog.DefaultAssetPath, created, existing);
        EnsureAsset<FishingLootCatalog>(FishingLootCatalog.DefaultAssetPath, created, existing);
        EnsureAsset<PlantOverlaySpriteCatalog>(
            PlantOverlaySpriteCatalog.DefaultAssetPath,
            created,
            existing);
        EnsureAsset<ArmAnimSlotCatalog>(ArmAnimSlotCatalog.DefaultAssetPath, created, existing);
        EnsureAsset<WeaponPresentationCatalog>(
            WeaponPresentationCatalog.DefaultAssetPath,
            created,
            existing);
        EnsureAsset<WeaponCombatFallbacks>(
            WeaponCombatFallbacks.DefaultAssetPath,
            created,
            existing);
        EnsureAsset<WeaponImpactVfxDefaults>(
            WeaponImpactVfxDefaults.DefaultAssetPath,
            created,
            existing);
        EnsureAsset<CharacterFactionCatalog>(
            CharacterFactionCatalog.DefaultAssetPath,
            created,
            existing);
        EnsureAsset<TraitIconCatalog>(TraitIconCatalog.DefaultAssetPath, created, existing);
        MirrorToResources<TraitIconCatalog>(
            TraitIconCatalog.DefaultAssetPath,
            TraitIconCatalog.ResourcesAssetPath,
            created,
            existing);
        EnsureAsset<ItemIconCatalog>(ItemIconCatalog.DefaultAssetPath, created, existing);
        EnsureAsset<LocalizationTable>(LocalizationTable.DefaultAssetPath, created, existing);
        EnsureAsset<LocalizationBundle>(LocalizationBundle.DefaultAssetPath, created, existing);
        EnsureAsset<InventoryListColumnLayoutSettings>(
            InventoryListColumnLayoutSettings.DefaultAssetPath,
            created,
            existing);

        var report = new StringBuilder();
        report.AppendLine($"[DistSoSampleEnsureMenu] created={created.Count} existing={existing.Count}");
        for (int i = 0; i < created.Count; i++)
            report.AppendLine("  + " + created[i]);
        for (int i = 0; i < existing.Count; i++)
            report.AppendLine("  = " + existing[i]);
        Debug.Log(report.ToString().TrimEnd());
    }

    static void EnsureAsset<T>(
        string assetPath,
        List<string> created,
        List<string> existing) where T : ScriptableObject
    {
        if (string.IsNullOrEmpty(assetPath))
            return;

        bool hadAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath) != null;
        T asset = DistScriptableObjectEnsure.LoadOrCreate<T>(assetPath);
        if (asset == null)
            return;

        if (hadAsset)
            existing.Add(assetPath);
        else
            created.Add(assetPath);

        EditorUtility.SetDirty(asset);
    }

    static void MirrorToResources<T>(
        string sourcePath,
        string resourcesPath,
        List<string> created,
        List<string> existing) where T : ScriptableObject
    {
        if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(resourcesPath))
            return;

        T source = AssetDatabase.LoadAssetAtPath<T>(sourcePath);
        if (source == null)
            return;

        bool hadAsset = AssetDatabase.LoadAssetAtPath<T>(resourcesPath) != null;
        DistScriptableObjectEnsure.EnsureParentFoldersForAsset(resourcesPath);
        if (!hadAsset)
        {
            if (!AssetDatabase.CopyAsset(sourcePath, resourcesPath))
                return;
            created.Add(resourcesPath + " (resources mirror)");
        }
        else
        {
            EditorUtility.CopySerialized(source, AssetDatabase.LoadAssetAtPath<T>(resourcesPath));
            existing.Add(resourcesPath + " (resources mirror)");
        }

        T mirrored = AssetDatabase.LoadAssetAtPath<T>(resourcesPath);
        if (mirrored != null)
            EditorUtility.SetDirty(mirrored);
    }
}
#endif
