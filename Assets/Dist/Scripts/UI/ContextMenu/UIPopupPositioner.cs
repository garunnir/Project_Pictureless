// ============================================================
// UIPopupPositioner — 팝업 UI의 마우스 위치 기준 배치 SSOT
// ============================================================

using UnityEngine;

public static class UIPopupPositioner
{
    /// <summary>부모 로컬에 배치. clamp 없음 (ContextMenu 패리티).</summary>
    public static void PlaceAtScreenPoint(RectTransform panel, Vector2 screenPosition, Canvas rootCanvas)
    {
        PlaceAtScreenPoint(panel, screenPosition, rootCanvas, Vector2.zero, clampToCanvas: false);
    }

    /// <summary>
    /// 스크린 포인트(+offset)를 부모 로컬로 배치.
    /// clampToCanvas=true면 패널 rect가 루트 캔버스 rect 밖으로 나가지 않게 보정 (호버 SSOT).
    /// </summary>
    public static void PlaceAtScreenPoint(
        RectTransform panel,
        Vector2 screenPosition,
        Canvas rootCanvas,
        Vector2 screenOffset,
        bool clampToCanvas)
    {
        if (panel == null)
            return;

        RectTransform parent = panel.parent as RectTransform;
        if (parent == null)
            return;

        Camera camera = ResolveCamera(rootCanvas);
        Vector2 screen = screenPosition + screenOffset;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, screen, camera, out Vector2 localPoint))
            return;

        if (clampToCanvas && rootCanvas != null)
        {
            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            if (canvasRect != null)
                localPoint = ClampLocalToCanvas(panel, parent, canvasRect, localPoint);
        }

        panel.anchoredPosition = localPoint;
    }

    public static Camera ResolveCamera(Canvas canvas) =>
        canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

    static Vector2 ClampLocalToCanvas(
        RectTransform panel,
        RectTransform parent,
        RectTransform canvasRect,
        Vector2 desiredParentLocal)
    {
        Vector3[] corners = new Vector3[4];
        canvasRect.GetWorldCorners(corners);
        Vector2 canvasMin = parent.InverseTransformPoint(corners[0]);
        Vector2 canvasMax = parent.InverseTransformPoint(corners[2]);

        Vector2 size = panel.rect.size;
        Vector2 pivot = panel.pivot;

        float minX = canvasMin.x + size.x * pivot.x;
        float maxX = canvasMax.x - size.x * (1f - pivot.x);
        float minY = canvasMin.y + size.y * pivot.y;
        float maxY = canvasMax.y - size.y * (1f - pivot.y);

        if (minX > maxX)
            desiredParentLocal.x = (minX + maxX) * 0.5f;
        else
            desiredParentLocal.x = Mathf.Clamp(desiredParentLocal.x, minX, maxX);

        if (minY > maxY)
            desiredParentLocal.y = (minY + maxY) * 0.5f;
        else
            desiredParentLocal.y = Mathf.Clamp(desiredParentLocal.y, minY, maxY);

        return desiredParentLocal;
    }
}
