// ============================================================
// GameDataWeaponPresentationEditor — GameData → 비주얼 허브 진입
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

    static bool _foldVisualHub;
    static bool _foldWeaponPresentation;

    /// <summary>
    /// Combat Presentation 카테고리 안 내용. 부모 foldout은 GameDataEditorWindow가 담당.
    /// </summary>
    public static void DrawSection(ItemData item, bool editable)
    {
        if (item == null || string.IsNullOrEmpty(item.id))
            return;

        WeaponPresentationCatalog catalog =
            AssetDatabase.LoadAssetAtPath<WeaponPresentationCatalog>(CatalogPath);

        DrawVisualHubSection(catalog);
        DrawWeaponPresentationBody(item, editable, catalog);
    }

    /// <summary>허브 SO만 연다. 잎 편집은 Catalog Odin 탭에서.</summary>
    public static void DrawVisualHubSection(WeaponPresentationCatalog catalog)
    {
        _foldVisualHub = EditorGUILayout.Foldout(
            _foldVisualHub,
            "Visual Hub",
            true,
            EditorStyles.foldoutHeader);
        if (!_foldVisualHub)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.HelpBox(
            "WeaponPresentationCatalog 한곳에서 Pipeline / Tag VFX / 바인딩을 탭·인라인 편집.",
            MessageType.None);

        if (catalog == null)
        {
            EditorGUILayout.HelpBox($"Catalog missing: {CatalogPath}", MessageType.Warning);
            EditorGUI.indentLevel--;
            return;
        }

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField(
            "Catalog",
            catalog,
            typeof(WeaponPresentationCatalog),
            false);
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Open Visual Hub", GUILayout.Width(140)))
            Selection.activeObject = catalog;

        EditorGUI.indentLevel--;
    }

    static void DrawWeaponPresentationBody(
        ItemData item,
        bool editable,
        WeaponPresentationCatalog catalog)
    {
        _foldWeaponPresentation = EditorGUILayout.Foldout(
            _foldWeaponPresentation,
            "Weapon Presentation (this item)",
            true,
            EditorStyles.foldoutHeader);
        if (!_foldWeaponPresentation)
            return;

        EditorGUI.indentLevel++;

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
