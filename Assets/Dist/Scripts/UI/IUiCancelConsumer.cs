// ============================================================
// IUiCancelConsumer — UiCancelRouter ESC 소비자 계약
// ============================================================

public interface IUiCancelConsumer
{
    int CancelPriority { get; }

    /// <summary>true = 이번 프레임 consume. 나머지 소비자 스킵.</summary>
    bool TryHandleCancel();
}
