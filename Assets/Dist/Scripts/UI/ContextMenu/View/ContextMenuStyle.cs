// ============================================================
// ContextMenuStyle — 캐스케이드 메뉴 시각·타이밍 SSOT
// ============================================================

using UnityEngine;

public static class ContextMenuStyle
{
    public static readonly Color PanelColor = new(0.12f, 0.12f, 0.12f, 0.95f);
    public static readonly Color RowColor = new(0.22f, 0.22f, 0.22f, 1f);
    public static readonly Color RowHoverColor = new(0.35f, 0.45f, 0.55f, 1f);
    public static readonly Color RowDisabledColor = new(0.18f, 0.18f, 0.18f, 0.7f);

    public const float MinPanelWidth = 140f;
    public const float MaxPanelWidth = 360f;
    public const float RowHeight = 28f;
    public const float PanelMaxHeight = 320f;
    public const float PanelPadding = 4f;
    public const float RowSpacing = 2f;
    public const float RowPaddingLeft = 6f;
    public const float RowPaddingRight = 4f;
    public const float RowLabelChevronGap = 4f;
    public const float SubmenuGap = 2f;
    public const float CloseDelaySeconds = 0.25f;
    public const int FontSize = 14;
    public const float ChevronWidth = 18f;
}
