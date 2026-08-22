// ============================================================
// MessageLogNeedsSink — 구토·아사/탈수·6시간 허기/갈증 경고만 메시지 로그
// ============================================================
// flowchart LR
//   Host[PlayerNeedsHost events] --> Sink[MessageLogNeedsSink]
//   Sink --> Log[GameplayMessageLog]

using UnityEngine;

[DisallowMultipleComponent]
public sealed class MessageLogNeedsSink : MonoBehaviour
{
    public const string LocVomit = "msg.status.needs_vomit";
    public const string LocStarve = "msg.status.needs_starve";
    public const string LocDehydrate = "msg.status.needs_dehydrate";
    public const string LocHunger70 = "msg.status.needs_hunger_70";
    public const string LocHunger50 = "msg.status.needs_hunger_50";
    public const string LocHunger25 = "msg.status.needs_hunger_25";
    public const string LocHunger10 = "msg.status.needs_hunger_10";
    public const string LocThirstDanger = "msg.status.needs_thirst_danger";

    bool _subscribed;

    void OnEnable() => EnsureSubscribed();

    void Start() => EnsureSubscribed();

    void OnDisable() => Unsubscribe();

    void OnDestroy() => Unsubscribe();

    void EnsureSubscribed()
    {
        if (_subscribed)
            return;

        PlayerNeedsHost.AnyNeedsVomit += OnVomit;
        PlayerNeedsHost.AnyNeedsFatal += OnFatal;
        PlayerNeedsHost.AnyNeedsWarning += OnWarning;
        _subscribed = true;
    }

    void Unsubscribe()
    {
        if (!_subscribed)
            return;

        PlayerNeedsHost.AnyNeedsVomit -= OnVomit;
        PlayerNeedsHost.AnyNeedsFatal -= OnFatal;
        PlayerNeedsHost.AnyNeedsWarning -= OnWarning;
        _subscribed = false;
    }

    static void OnVomit()
    {
        GameplayMessageLog.Append(
            MessageLogCategory.Status,
            MessageLogImportance.Normal,
            Loc.Get(LocVomit));
    }

    static void OnFatal(NeedsFatalKind kind)
    {
        string key = kind == NeedsFatalKind.Dehydrate ? LocDehydrate : LocStarve;
        GameplayMessageLog.Append(
            MessageLogCategory.Status,
            MessageLogImportance.Critical,
            Loc.Get(key));
    }

    static void OnWarning(NeedsWarningKind kind)
    {
        string key;
        switch (kind)
        {
            case NeedsWarningKind.Hunger10:
                key = LocHunger10;
                break;
            case NeedsWarningKind.Hunger25:
                key = LocHunger25;
                break;
            case NeedsWarningKind.Hunger50:
                key = LocHunger50;
                break;
            case NeedsWarningKind.Hunger70:
                key = LocHunger70;
                break;
            default:
                key = LocThirstDanger;
                break;
        }

        GameplayMessageLog.Append(
            MessageLogCategory.Status,
            MessageLogImportance.Normal,
            Loc.Get(key));
    }
}
