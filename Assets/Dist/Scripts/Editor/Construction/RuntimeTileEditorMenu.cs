// ============================================================
// RuntimeTileEditorMenu — 런타임 편집기 디버그 토글
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class RuntimeTileEditorMenu
{
    [MenuItem(DistMcpMenus.ConstructionToggleRuntimeEditor)]
    static void ToggleRuntimeEditor()
    {
        UIConstruction ui = Object.FindAnyObjectByType<UIConstruction>(FindObjectsInactive.Include);
        if (ui == null)
        {
            Debug.LogError(
                "[RuntimeTileEditorMenu] UIConstruction not found in open scene.");
            return;
        }

        if (ui.gameObject.activeSelf)
            ui.Close();
        else
            ui.Open();
    }
}
#endif
