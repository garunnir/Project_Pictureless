// ============================================================
// UIPopupPositioner — 팝업 UI의 마우스 위치 기준 배치 SSOT
// ============================================================

using UnityEngine;

public static class UIPopupPositioner
{
    public static void PlaceAtScreenPoint(RectTransform panel, Vector2 screenPosition, Canvas rootCanvas)
    {
        if (panel == null) return;

        RectTransform parent = panel.parent as RectTransform;
        if (parent == null) return;

        Camera camera = ResolveCamera(rootCanvas);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent, screenPosition, camera, out Vector2 localPoint);

        panel.anchoredPosition = localPoint;
    }

    public static Camera ResolveCamera(Canvas canvas) =>
        canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
}
