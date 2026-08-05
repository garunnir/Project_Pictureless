// ============================================================
// UIHoverStyle — 호버 정보창 Placement 파라미터 (offset·follow; clamp는 Positioner SSOT)
// ============================================================

using UnityEngine;

public readonly struct UIHoverStyle
{
    public readonly Vector2 ScreenOffset;
    public readonly bool FollowMouse;

    public UIHoverStyle(Vector2 screenOffset, bool followMouse)
    {
        ScreenOffset = screenOffset;
        FollowMouse = followMouse;
    }
}
