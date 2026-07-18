// ============================================================
// ContextMenuChromeLabels — 공용 컨텍스트 메뉴 chrome 문구 SSOT
// ============================================================

public static class ContextMenuChromeLabels
{
    /// <summary>로케일 키는 기존 ItemContextMenu 테이블과 공유 (하위 호환).</summary>
    public const string KeySubmenuChevron = "ItemContextMenu.SubmenuChevron";

    public static string SubmenuChevron => Loc.Get(KeySubmenuChevron);
}
