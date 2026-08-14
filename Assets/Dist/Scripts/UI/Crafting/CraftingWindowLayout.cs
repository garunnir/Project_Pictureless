// ============================================================
// CraftingWindowLayout — 제작 창 크기 clamp·셀 치수 SSOT
// ============================================================

using UnityEngine;

public static class CraftingWindowLayout
{
    public const float MinWidth = 720f;
    public const float MinHeight = 480f;
    public const float DefaultWidth = 720f;
    public const float DefaultHeight = 480f;
    public const float MaxCanvasWidthRatio = 0.85f;
    public const float MaxCanvasHeightRatio = 0.85f;

    public const float HeaderHeight = 32f;
    public const float CategoryColumnWidth = 168f;
    public const float DetailColumnWidth = 268f;
    public const float SearchRowHeight = 28f;
    public const float CategoryRowHeight = 28f;
    public const float RecipeCellSize = 72f;
    public const float RecipeListRowHeight = 32f;
    public const float IngredientCellSize = 56f;
    public const float IngredientSwapWidth = 20f;
    public const float IngredientGridSpacing = 4f;
    public const float IngredientKindBadgeSize = 12f;
    public const float IngredientCountWidth = 36f;
    public const float IngredientCountHeight = 16f;
    public const float IngredientOverlayInset = 2f;
    public const float OutputsScrollMinHeight = 56f;
    public const float FooterQtyRowHeight = 28f;
    public const float FooterTimeRowHeight = 18f;
    public const float FooterProgressHeight = 10f;
    public const float QtyButtonWidth = 28f;
    public const float QtyInputWidth = 48f;
    public const float QtyMaxButtonWidth = 48f;
    public const float FooterCraftButtonHeight = 32f;
    public const float FooterSpacing = 4f;
    public const float FooterPadding = 4f;
    public const int MaxCraftQuantity = 99;
    public const float SecondsPerMinute = 60f;
    public const float UnmetIconAlpha = 0.4f;
    public const float ResultIconSize = 48f;
    public const float CloseButtonSize = 24f;
    public const float ChromePadding = 8f;
    public const float ColumnSpacing = 6f;
    public const float ResizeEdgeThickness = 8f;
    public const float VisibleRowBuffer = 2f;

    public const int FontSizeHeader = 18;
    public const int FontSizeBody = 14;
    public const int FontSizeSmall = 12;

    public static readonly Color PanelColor = new(0.12f, 0.12f, 0.12f, 0.95f);
    public static readonly Color HeaderColor = new(0.16f, 0.16f, 0.16f, 1f);
    public static readonly Color ColumnColor = new(0.1f, 0.1f, 0.1f, 1f);
    public static readonly Color RowColor = new(0.18f, 0.18f, 0.18f, 1f);
    public static readonly Color SelectedColor = new(0.25f, 0.35f, 0.45f, 1f);
    public static readonly Color ButtonColor = new(0.22f, 0.22f, 0.24f, 1f);
    public static readonly Color SkillMetColor = new(0.45f, 0.85f, 0.5f, 1f);
    public static readonly Color SkillUnmetColor = new(0.9f, 0.35f, 0.35f, 1f);
    public static readonly Color ConsumeIconColor = new(0.75f, 0.28f, 0.22f, 1f);
    public static readonly Color KeepIconColor = new(0.35f, 0.5f, 0.7f, 1f);
    public static readonly Color FuelIconColor = new(0.85f, 0.55f, 0.2f, 1f);

    public static Vector2 DefaultSize => new(DefaultWidth, DefaultHeight);

    public static Vector2 IngredientGridCellSize =>
        new(IngredientCellSize + IngredientSwapWidth, IngredientCellSize);

    public static Vector2 GetMaxSize(Canvas canvas)
    {
        if (canvas == null)
            return new Vector2(1280f, 900f);

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
