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

    public static void DrawSection(ItemData item, bool editable)
    {
        if (item == null || string.IsNullOrEmpty(item.id))
            return;

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Weapon Presentation", EditorStyles.boldLabel);

        WeaponPresentationCatalog catalog =
            AssetDatabase.LoadAssetAtPath<WeaponPresentationCatalog>(CatalogPath);
        if (catalog == null)
        {
            EditorGUILayout.HelpBox(
                $"Catalog missing: {CatalogPath}",
                MessageType.Warning);
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
