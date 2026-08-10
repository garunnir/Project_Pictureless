// ============================================================
// GameDataWeaponPresentationEditor — GameData 아이템↔연출 Ensure/Edit
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEditor;
using UnityEngine;

static class GameDataWeaponPresentationEditor
{
    public const string CatalogPath =
        "Assets/Dist/SOData/Combat/WeaponPresentations/WeaponPresentationCatalog.asset";
    public const string PresentationsFolder =
        "Assets/Dist/SOData/Combat/WeaponPresentations";
    public const string ActionVfxDefaultsPath =
        "Assets/Dist/SOData/Combat/WeaponPresentations/WeaponActionVfxDefaults.asset";

    static bool _foldActionVfxFallback;
    static bool _foldWeaponPresentation;

    /// <summary>
    /// Combat Presentation 카테고리 안 내용. 부모 foldout은 GameDataEditorWindow가 담당.
    /// </summary>
    public static void DrawSection(ItemData item, bool editable)
    {
        if (item == null || string.IsNullOrEmpty(item.id))
            return;

        DrawActionVfxFallbackSection();
        DrawWeaponPresentationBody(item, editable);
    }

    /// <summary>전역 태그 VFX 폴백 진입점 (아이템 전용 아님). 기본 접힘.</summary>
    public static void DrawActionVfxFallbackSection()
    {
        _foldActionVfxFallback = EditorGUILayout.Foldout(
            _foldActionVfxFallback,
            "Action VFX Fallback (global tag defaults)",
            true,
            EditorStyles.foldoutHeader);
        if (!_foldActionVfxFallback)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.HelpBox(
            "Not item-specific. Empty WeaponPresentation Entry VFX slots fall back here. Not stored in ItemData JSON.",
            MessageType.Info);

        WeaponActionVfxDefaults defaults =
            AssetDatabase.LoadAssetAtPath<WeaponActionVfxDefaults>(ActionVfxDefaultsPath);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField(
            "Defaults",
            defaults,
            typeof(WeaponActionVfxDefaults),
            false);
        EditorGUI.EndDisabledGroup();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (defaults != null && GUILayout.Button("Edit Defaults", GUILayout.Width(120)))
                Selection.activeObject = defaults;
            else if (defaults == null)
                EditorGUILayout.HelpBox($"Missing: {ActionVfxDefaultsPath}", MessageType.Warning);
        }

        EditorGUI.indentLevel--;
    }

    static void DrawWeaponPresentationBody(ItemData item, bool editable)
    {
        _foldWeaponPresentation = EditorGUILayout.Foldout(
            _foldWeaponPresentation,
            "Weapon Presentation (per item)",
            true,
            EditorStyles.foldoutHeader);
        if (!_foldWeaponPresentation)
            return;

        EditorGUI.indentLevel++;

        WeaponPresentationCatalog catalog =
            AssetDatabase.LoadAssetAtPath<WeaponPresentationCatalog>(CatalogPath);
        if (catalog == null)
        {
            EditorGUILayout.HelpBox(
                $"Catalog missing: {CatalogPath}",
                MessageType.Warning);
            EditorGUI.indentLevel--;
            return;
        }

        catalog.TryGetByItemId(item.id, out WeaponPresentation bound);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField("Bound", bound, typeof(WeaponPresentation), false);
        EditorGUI.EndDisabledGroup();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (editable && GUILayout.Button("Ensure Presentation", GUILayout.Width(140)))
            {
                WeaponPresentation created = EnsurePresentationAsset(item.id);
                catalog.EnsureItemBinding(item.id, created);
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
                Selection.activeObject = created;
            }

            if (bound != null && GUILayout.Button("Edit Presentation", GUILayout.Width(130)))
                Selection.activeObject = bound;

            if (editable && bound != null && GUILayout.Button("Unlink", GUILayout.Width(70)))
            {
                catalog.UnlinkItem(item.id);
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }
        }

        EditorGUI.indentLevel--;
    }

    static WeaponPresentation EnsurePresentationAsset(string itemId)
    {
        string path = $"{PresentationsFolder}/Weapon_{SanitizeFileName(itemId)}.asset";
        WeaponPresentation existing =
            AssetDatabase.LoadAssetAtPath<WeaponPresentation>(path);
        if (existing != null)
            return existing;

        var created = ScriptableObject.CreateInstance<WeaponPresentation>();
        AssetDatabase.CreateAsset(created, path);
        return created;
    }

    static string SanitizeFileName(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "Item";
        return id.Replace('/', '_').Replace('\\', '_').Replace(':', '_');
    }
}
