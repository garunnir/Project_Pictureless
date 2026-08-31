// ============================================================
// GameDataWeaponPresentationEditor — GameData → 비주얼 허브 진입
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEditor;
using UnityEngine;

static class GameDataWeaponPresentationEditor
{
    public const string CatalogPath = WeaponPresentationCatalog.DefaultAssetPath;
    public const string PresentationsFolder =
        "Assets/Dist/SOData/Combat/Presentations";

    static bool _foldWeaponPresentation;

    /// <summary>
    /// 아이템 전용 Presentation 바인딩만. Fallbacks·Attack 전체는 Data Definitions → Combat.
    /// </summary>
    public static void DrawItemBindingSection(ItemData item, bool editable)
    {
        if (item == null || string.IsNullOrEmpty(item.id))
            return;

        WeaponPresentationCatalog catalog =
            AssetDatabase.LoadAssetAtPath<WeaponPresentationCatalog>(CatalogPath);
        DrawWeaponPresentationBody(item, editable, catalog);
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

        EditorGUILayout.HelpBox(
            "이 아이템 전용 동작 목록입니다. 비어 있으면 카탈로그가 gun.skill, " +
            "그래도 없으면 weapon_category, 그래도 없으면 맨손을 씁니다.\n" +
            "Ensure Presentation: 전용 에셋을 만들고 연결합니다. Unlink: 연결만 끊고 에셋은 남깁니다.",
            MessageType.None);

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
