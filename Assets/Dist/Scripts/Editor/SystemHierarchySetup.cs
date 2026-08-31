// ============================================================
// SystemHierarchySetup — System 루트 카테고리 폴더 SSOT (에디터 Setup용)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class SystemHierarchySetup
{
    public const string Time = "Time";
    public const string PlayerStatus = "PlayerStatus";
    public const string Inventory = "Inventory";
    public const string Combat = "Combat";
    public const string Msg = "Msg";
    public const string Crafting = "Crafting";
    public const string Construction = "Construction";

    public const string Settings = "Settings";

    public static Transform ResolveSystemRoot()
    {
        InputManager inputManager = Object.FindAnyObjectByType<InputManager>();
        return inputManager != null ? inputManager.transform.parent : null;
    }

    public static Transform EnsureCategory(Transform systemRoot, string categoryName)
    {
        if (systemRoot == null || string.IsNullOrEmpty(categoryName))
            return systemRoot;

        Transform existing = systemRoot.Find(categoryName);
        if (existing != null)
            return existing;

        GameObject go = new(categoryName);
        Undo.RegisterCreatedObjectUndo(go, $"Create System/{categoryName}");
        go.transform.SetParent(systemRoot, false);
        return go.transform;
    }

    public static void EnsureChildUnder(
        Transform category,
        Transform child,
        string undoLabel)
    {
        if (category == null || child == null || child.parent == category)
            return;

        Undo.SetTransformParent(child, category, undoLabel);
        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
    }
}
#endif
