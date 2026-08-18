// ============================================================
// SettingsWindowLayout — 세팅 Overlay 패널 치수 SSOT
// ============================================================

using UnityEngine;

public static class SettingsWindowLayout
{
    public const float PanelWidth = 420f;
    public const float PanelHeight = 360f;
    public const float CategoryColumnWidth = 120f;
    public const float HeaderHeight = 32f;
    public const float ChromePadding = 8f;
    public const float ToggleRowHeight = 28f;
    public const float HudPopupToggleInset = 20f;
    public const float ToggleStackSpacing = 4f;
    public const int FontSizeBody = 14;
    public const int FontSizeHudPopup = 12;
    public const int FontSizeHeader = 16;
    public static readonly Vector2 AnchoredPosition = new(16f, 0f);
    public static readonly Color PanelColor = new(0.1f, 0.1f, 0.12f, 0.96f);
    public static readonly Color HeaderColor = new(0.16f, 0.16f, 0.18f, 1f);
    public static readonly Color CategoryColor = new(0.14f, 0.14f, 0.16f, 1f);
    public static readonly Color ContentColor = new(0.12f, 0.12f, 0.14f, 1f);
}
