// ============================================================
// MessageLogPlayerEncumbranceSink — Extreme 과적 이동 시도만 메시지 로그
// ============================================================

using UnityEngine;

[DisallowMultipleComponent]
public sealed class MessageLogPlayerEncumbranceSink : MonoBehaviour
{
    const string LocImmobile = "msg.status.encumbrance_immobile";

    bool _subscribed;
    bool _loggedThisExtreme;

    void OnEnable() => EnsureSubscribed();

    void Start() => EnsureSubscribed();

    void OnDisable() => Unsubscribe();

    void OnDestroy() => Unsubscribe();

    void EnsureSubscribed()
    {
        if (_subscribed)
            return;

        PlayerMovement.AnyImmobileMoveAttempted += OnImmobileMoveAttempted;
        PlayerEncumbranceHost.StageChanged += OnEncumbranceStageChanged;
        _subscribed = true;
        SyncLoggedFlag();
    }

    void Unsubscribe()
    {
        if (!_subscribed)
            return;

        PlayerMovement.AnyImmobileMoveAttempted -= OnImmobileMoveAttempted;
        PlayerEncumbranceHost.StageChanged -= OnEncumbranceStageChanged;
        _subscribed = false;
    }

    void OnEncumbranceStageChanged() => SyncLoggedFlag();

    void SyncLoggedFlag()
    {
        PlayerEncumbranceHost host = PlayerEncumbranceHost.Active;
        if (host == null || host.Stage != PlayerEncumbranceStage.Extreme)
            _loggedThisExtreme = false;
    }

    void OnImmobileMoveAttempted()
    {
        PlayerEncumbranceHost host = PlayerEncumbranceHost.Active;
        if (host == null || host.Stage != PlayerEncumbranceStage.Extreme)
            return;

        if (_loggedThisExtreme)
            return;

        _loggedThisExtreme = true;
        GameplayMessageLog.Append(
            MessageLogCategory.Status,
            MessageLogImportance.Normal,
            Loc.Get(LocImmobile));
    }
}
