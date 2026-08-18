// ============================================================
// TimeScaleHudLayout — 배속 HUD 우상단 배치 SSOT
// ============================================================

using UnityEngine;

public static class TimeScaleHudLayout
{
    public const string ParticipantId = HudLayoutIds.TimeScaleHud;

    public static readonly Vector2 PanelSize = new(168f, 32f);
    public static readonly Vector2 AnchoredPosition = new(-268f, -12f);
    public const float ButtonSpacing = 4f;
    public const float ButtonSize = 28f;
    public const int FontSize = 13;
    public static readonly Color PanelColor = new(0.1f, 0.1f, 0.12f, 0.72f);
    public static readonly Color SelectedColor = new(0.28f, 0.32f, 0.4f, 1f);
    public static readonly Color NormalColor = new(0.18f, 0.18f, 0.2f, 1f);
}
