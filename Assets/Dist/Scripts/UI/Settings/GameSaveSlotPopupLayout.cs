// ============================================================
// GameSaveSlotPopupLayout — 슬롯 팝업 chrome 치수 SSOT
// ============================================================

using UnityEngine;

public static class GameSaveSlotPopupLayout
{
    public const float PanelWidth = 360f;
    public const float PanelHeight = 420f;
    public const float HeaderHeight = 32f;
    public const float ChromePadding = 8f;
    public const float SlotRowHeight = 32f;
    public const float SlotStackSpacing = 4f;
    public const float ConfirmPanelHeight = 120f;
    public const float ActionButtonHeight = 28f;
    public const float ActionButtonSpacing = 8f;
    public const int FontSizeHeader = 16;
    public const int FontSizeBody = 14;
    public const int FontSizeSubtitle = 11;
    public static readonly Color BackdropColor = new(0f, 0f, 0f, 0.55f);
    public static readonly Color PanelColor = new(0.1f, 0.1f, 0.12f, 0.98f);
    public static readonly Color HeaderColor = new(0.16f, 0.16f, 0.18f, 1f);
    public static readonly Color SlotColor = new(0.14f, 0.14f, 0.16f, 1f);
    public static readonly Color SlotEmptyColor = new(0.12f, 0.12f, 0.13f, 0.85f);
    public static readonly Color ConfirmColor = new(0.08f, 0.08f, 0.1f, 0.92f);
}
