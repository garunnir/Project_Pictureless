// ============================================================
// ContextMenuOutsideClick — 열린 cascade 패널 밖 포인터 판정 SSOT
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class ContextMenuOutsideClick
{
    public static bool TryGetPressScreenPosition(out Vector2 screenPosition)
    {
        screenPosition = default;
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return false;

        if (!mouse.leftButton.wasPressedThisFrame && !mouse.rightButton.wasPressedThisFrame)
            return false;

        screenPosition = mouse.position.ReadValue();
        return true;
    }

    public static bool IsOverAnyPanel(
        IReadOnlyList<UIContextMenuCascadePanel> panels,
        Vector2 screenPosition,
        Camera uiCamera)
    {
        if (panels == null)
            return false;

        for (int i = 0; i < panels.Count; i++)
        {
            UIContextMenuCascadePanel panel = panels[i];
            if (panel == null)
                continue;

            RectTransform root = panel.Root;
            if (root == null || !root.gameObject.activeInHierarchy)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(root, screenPosition, uiCamera))
                return true;
        }

        return false;
    }
}
