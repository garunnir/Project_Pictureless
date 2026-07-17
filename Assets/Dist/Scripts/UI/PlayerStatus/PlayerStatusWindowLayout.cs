// ============================================================
// PlayerStatusWindowLayout — 상태창 크기 clamp SSOT
// ============================================================

using UnityEngine;

public static class PlayerStatusWindowLayout
{
    public const float MinWidth = 280f;
    public const float MinHeight = 360f;
    public const float MaxCanvasWidthRatio = 0.55f;
    public const float MaxCanvasHeightRatio = 0.85f;

    public static Vector2 GetMaxSize(Canvas canvas)
    {
        if (canvas == null)
            return new Vector2(720f, 900f);

        Vector2 canvasSize = ((RectTransform)canvas.transform).rect.size;
        return new Vector2(
            canvasSize.x * MaxCanvasWidthRatio,
            canvasSize.y * MaxCanvasHeightRatio);
    }

    public static Vector2 ClampSize(Vector2 size, Canvas canvas)
    {
        Vector2 max = GetMaxSize(canvas);
        size.x = Mathf.Clamp(size.x, MinWidth, max.x);
        size.y = Mathf.Clamp(size.y, MinHeight, max.y);
        return size;
    }
}
