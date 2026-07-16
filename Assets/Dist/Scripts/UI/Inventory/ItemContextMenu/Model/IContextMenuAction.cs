// ============================================================
// IContextMenuAction — 컨텍스트 메뉴 리프 실행 계약
// ============================================================

/// <summary>
/// 리프 항목의 활성 판정·실행. Model/View는 구현체를 모른다.
/// </summary>
public interface IContextMenuAction
{
    /// <summary>null이면 실행 가능. 비어 있지 않으면 비활성 사유.</summary>
    string GetDisabledReason();

    void Execute();
}
