// ============================================================
// UIWindowChromeLayout — 공용 창 헤더 접기/끄기 버튼 치수 SSOT
// ============================================================

using UnityEngine;

public static class UIWindowChromeLayout
{
    public const float ButtonSize = 18f;
    public const float ButtonSpacing = 2f;
    public const float ButtonEdgePadding = 4f;
    public const float FoldedHeaderHeight = 16f;
    public const int ButtonFontSize = 13;

    public const string FoldExpandedLabel = "−";
    public const string FoldCollapsedLabel = "+";
    public const string CloseLabel = "×";

    public static readonly Color ButtonColor = new(0.22f, 0.22f, 0.24f, 1f);

    public static float ClusterWidth(int buttonCount)
    {
        if (buttonCount <= 0)
            return 0f;

        return (ButtonSize * buttonCount)
            + (ButtonSpacing * (buttonCount - 1))
            + ButtonEdgePadding;
    }
}
