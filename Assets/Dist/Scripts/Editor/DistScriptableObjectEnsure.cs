// ============================================================
// DistScriptableObjectEnsure — Dist SSOT ScriptableObject 샘플 Ensure
// ============================================================

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

static class DistScriptableObjectEnsure
{
    public static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
    {
        if (string.IsNullOrEmpty(assetPath))
            return null;

        T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (existing != null)
            return existing;

        EnsureParentFoldersForAsset(assetPath);
        T created = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(created, assetPath);
        AssetDatabase.SaveAssets();
        return created;
    }

    public static void EnsureParentFoldersForAsset(string assetPath)
    {
        string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            return;

        string[] parts = folder.Split('/');
        if (parts.Length < 2 || parts[0] != "Assets")
            return;

        string current = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
