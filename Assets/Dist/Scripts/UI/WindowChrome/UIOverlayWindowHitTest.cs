// ============================================================
// UIOverlayWindowHitTest — 등록된 Dist 오버레이 창 Rect 위 포인터 판별 SSOT
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public static class UIOverlayWindowHitTest
{
    static readonly List<RectTransform> Windows = new(8);

    public static void Register(RectTransform window)
    {
        if (window == null)
            return;

        if (!Windows.Contains(window))
            Windows.Add(window);
    }

    public static void Unregister(RectTransform window)
    {
        if (window == null)
            return;

        Windows.Remove(window);
    }

    public static bool ContainsScreenPoint(Vector2 screenPosition, Camera uiCamera)
    {
        for (int i = Windows.Count - 1; i >= 0; i--)
        {
            RectTransform window = Windows[i];
            if (window == null)
            {
                Windows.RemoveAt(i);
                continue;
            }

            if (!window.gameObject.activeInHierarchy)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(window, screenPosition, uiCamera))
                return true;
        }

        return false;
    }
}
