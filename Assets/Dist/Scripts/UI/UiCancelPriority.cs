// ============================================================
// UiCancelPriority — ESC/Cancel 소비자 우선순위 SSOT (간격 넓게)
// ============================================================

public static class UiCancelPriority
{
    // 높을수록 먼저 처리. Settings는 ESC 폴백(열기/닫기) — 항상 맨 뒤.
    public const int ContextMenu = 100;
    public const int ModalPopup = 80;
    public const int FarmCellTarget = 75;
    public const int FishCellTarget = 74;
    public const int CharacterAction = 60;
    public const int OverlayWindow = 40;
    public const int Settings = -100;
}
