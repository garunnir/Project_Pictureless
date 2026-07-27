// ============================================================
// UIWindowResizeHandlesPrefabPatch — 프리팹에서 구 핸들 제거 + Handles 부착
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class UIWindowResizeHandlesPrefabPatch
{
    /// <summary>
    /// Area_ResizeHandle_* / UIWindowResizeHandler 자식 제거 후 UIWindowResizeHandles 배선.
    /// </summary>
    public static UIWindowResizeHandles Apply(
        GameObject windowRoot,
        float handleWidth,
        bool proximityReveal,
        Vector2 minSize,
        Vector2 maxSize)
    {
        if (windowRoot == null)
            return null;

        DestroyLegacyHandles(windowRoot);

        UIWindowResizeHandles host = windowRoot.GetComponent<UIWindowResizeHandles>();
        if (host == null)
            host = windowRoot.AddComponent<UIWindowResizeHandles>();

        RectTransform windowRect = windowRoot.transform as RectTransform;
        var so = new SerializedObject(host);
        so.FindProperty("_handleWidth").floatValue = Mathf.Max(1f, handleWidth);
        so.FindProperty("_proximityReveal").boolValue = proximityReveal;
        so.FindProperty("_window").objectReferenceValue = windowRect;
        so.FindProperty("_minSize").vector2Value = minSize;
        so.FindProperty("_maxSize").vector2Value = maxSize;
        so.ApplyModifiedPropertiesWithoutUndo();

        return host;
    }

    public static void DestroyLegacyHandles(GameObject windowRoot)
    {
        if (windowRoot == null)
            return;

        UIWindowResizeHandler[] handlers =
            windowRoot.GetComponentsInChildren<UIWindowResizeHandler>(true);
        for (int i = 0; i < handlers.Length; i++)
        {
            if (handlers[i] == null)
                continue;
            Object.DestroyImmediate(handlers[i].gameObject);
        }

        // 이름만 남은 빈 노드도 제거
        Transform root = windowRoot.transform;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.name.StartsWith("Area_ResizeHandle_"))
                Object.DestroyImmediate(child.gameObject);
        }
    }
}
#endif
