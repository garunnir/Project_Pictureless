// ============================================================
// UIHoverCanvasLayer — 호버 정보창 캔버스 레이어 SSOT
// ============================================================

using UnityEngine;

/// <summary>호버 패널 부모 레이어·sibling 정렬 SSOT. Presenter는 이 API만 사용.</summary>
public static class UIHoverCanvasLayer
{
    public const UICanvasLayer Layer = UICanvasLayer.TopMost;

    public static void EnsureParent(Transform panel, Canvas rootCanvas)
    {
        if (panel == null || rootCanvas == null)
            return;

        UICanvasLayerHost host = rootCanvas.GetComponent<UICanvasLayerHost>();
        Transform parent = host != null
            ? host.GetLayerRoot(Layer)
            : rootCanvas.transform;

        if (panel.parent != parent)
            panel.SetParent(parent, false);
    }

    public static void BringToFront(Transform panel)
    {
        if (panel != null)
            panel.SetAsLastSibling();
    }
}
