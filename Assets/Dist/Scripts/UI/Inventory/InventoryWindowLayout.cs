// ============================================================
// InventoryWindowLayout — 인벤 창 크기 제한 상수
// ============================================================

using UnityEngine;

public static class InventoryWindowLayout
{
    public const float HeaderHeight = 32f;
    public const float MinWidth = 320f;
    public const float MinHeight = 240f;
    public const float MaxCanvasWidthRatio = 0.75f;
    public const float MaxCanvasHeightRatio = 0.78f;

    public static readonly Vector2 DefaultPrimaryWindowSize = new(80f, 80f);

    public static Vector2 GetMaxSize(Canvas canvas)
    {
        if (canvas == null)
            return new Vector2(960f, 720f);

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
