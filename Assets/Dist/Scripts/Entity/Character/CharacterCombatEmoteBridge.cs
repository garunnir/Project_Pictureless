// ============================================================
// CharacterCombatEmoteBridge — NPC 전투/감각 경계 → 색채 느낌표
// ============================================================

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterEmoteHost))]
public sealed class CharacterCombatEmoteBridge : MonoBehaviour
{
    CharacterEmoteHost _host;
    CharacterMotor _motor;

    void Awake()
    {
        TryGetComponent(out _host);
        _motor = CharacterBodyResolve.GetInBody<CharacterMotor>(this);
    }

    public void SetAlertSpotted()
    {
        if (!CanApplyObserverEmote())
            return;

        _host.Request(new EmoteRequest(EmoteId.AlertSpotted, EmoteSource.Combat));
    }

    public void SetAlertSuspicious()
    {
        if (!CanApplyObserverEmote())
            return;

        _host.Request(new EmoteRequest(EmoteId.AlertSuspicious, EmoteSource.Combat));
    }

    public void ClearCombat() => _host?.Clear(EmoteSource.Combat);

    bool CanApplyObserverEmote() =>
        _host != null && (_motor == null || !_motor.IsPossessed);
}
