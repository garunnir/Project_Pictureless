// ============================================================
// CharacterMoodEmoteSource — possessed Mood → 월드 감정 이모트
// ============================================================

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterEmoteHost))]
public sealed class CharacterMoodEmoteSource : MonoBehaviour
{
    CharacterEmoteHost _host;
    CharacterMoodHost _moodHost;
    CharacterMotor _motor;

    void Awake()
    {
        TryGetComponent(out _host);
        TryGetComponent(out _moodHost);
        TryGetComponent(out _motor);
    }

    void OnEnable()
    {
        if (_moodHost != null)
            _moodHost.Changed += OnMoodChanged;
        Refresh();
    }

    void OnDisable()
    {
        if (_moodHost != null)
            _moodHost.Changed -= OnMoodChanged;
    }

    void OnMoodChanged() => Refresh();

    void Refresh()
    {
        if (_host == null)
            return;

        if (_motor == null || !_motor.IsPossessed || _moodHost == null)
        {
            _host.Clear(EmoteSource.Mood);
            return;
        }

        EmoteId id = CharacterMoodEmoteMapper.FromMood(_moodHost.Mood);
        _host.Request(new EmoteRequest(id, EmoteSource.Mood));
    }
}
