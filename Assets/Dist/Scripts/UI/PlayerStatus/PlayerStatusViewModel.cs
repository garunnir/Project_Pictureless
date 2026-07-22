// ============================================================
// PlayerStatusViewModel — Body/Vitals/Stats 참조 + 요약 무드 파생 상태
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public sealed class PlayerStatusViewModel
{
    IPlayerBody _body;
    IPlayerVitals _vitals;
    IPlayerStats _stats;

    readonly List<MoodEntry> _moodEntries = new(UIPlayerStatusSummaryPanel.MaxSlots);

    public event Action Changed;
    public event Action MoodChanged;

    public IPlayerBody Body => _body;
    public IPlayerVitals Vitals => _vitals;
    public IPlayerStats Stats => _stats;
    public IReadOnlyList<MoodEntry> MoodEntries => _moodEntries;

    public bool CanShowNumericVitals =>
        PlayerStatusVitalDisplay.CanShowNumericVitals(_stats);

    public void Bind(IPlayerBody body, IPlayerVitals vitals, IPlayerStats stats)
    {
        Unbind();
        _body = body;
        _vitals = vitals;
        _stats = stats;

        if (_body != null)
            _body.Changed += OnBodyChanged;
        if (_vitals != null)
            _vitals.Changed += OnVitalsChanged;
        if (_stats != null)
            _stats.Changed += OnStatsChanged;

        RebuildMoodEntries();
        Changed?.Invoke();
        MoodChanged?.Invoke();
    }

    public void Unbind()
    {
        if (_body != null)
            _body.Changed -= OnBodyChanged;
        if (_vitals != null)
            _vitals.Changed -= OnVitalsChanged;
        if (_stats != null)
            _stats.Changed -= OnStatsChanged;

        _body = null;
        _vitals = null;
        _stats = null;
        _moodEntries.Clear();
    }

    void OnBodyChanged()
    {
        RebuildMoodEntries();
        Changed?.Invoke();
        MoodChanged?.Invoke();
    }

    void OnVitalsChanged(string _)
    {
        RebuildMoodEntries();
        Changed?.Invoke();
        MoodChanged?.Invoke();
    }

    void OnStatsChanged(string _) => Changed?.Invoke();

    void RebuildMoodEntries() =>
        PlayerStatusMoodEntries.Collect(_body, _vitals, _moodEntries);
}
