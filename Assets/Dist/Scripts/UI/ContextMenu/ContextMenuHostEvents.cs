// ============================================================
// ContextMenuHostEvents — 컨텍스트 메뉴 호스트 상호 Hide
// ============================================================

using System;

public static class ContextMenuHostEvents
{
    public static event Action HideRequested;

    public static void RequestHide() => HideRequested?.Invoke();
}
