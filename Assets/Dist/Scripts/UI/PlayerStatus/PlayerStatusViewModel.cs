// ============================================================
// PlayerStatusViewModel ? Body/Vitals/Stats ?? + ?? ?? ?? ??
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public sealed class PlayerStatusViewModel
{
    ICharacterBody _body;
    IPlayerVitals _vitals;
    IPlayerStats _stats;

    readonly List<MoodEntry> _moodEntries = new(UIPlayerStatusSummaryPanel.MaxSlots);

    public event Action Changed;
    public event Action MoodChanged;

    public ICharacterBody Body => _body;
    public IPlayerVitals Vitals => _vitals;
    public IPlayerStats Stats => _stats;
    public IReadOnlyList<MoodEntry> MoodEntries => _moodEntries;

    public bool CanShowNumericVitals =>
        PlayerStatusVitalDisplay.CanShowNumericVitals(_stats);

    public void Bind(ICharacterBody body, IPlayerVitals vitals, IPlayerStats stats)
    {
        Unbind();
        _body = body;
        _vitals = vitals;
        _stats = stats;

        if (_stats is DefaultPlayerStats dps)
            dps.BindBody(_body);

        if (_body != null)
            _body.Changed += OnBodyChanged;
        if (_vitals != null)
            _vitals.Changed += OnVitalsChanged;
        if (_stats != null)
            _stats.Changed += OnStatsChanged;

        PlayerEncumbranceHost.StageChanged += OnEncumbranceChanged;
        PlayerEncumbranceHost.ActiveChanged += OnEncumbranceChanged;

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

        PlayerEncumbranceHost.StageChanged -= OnEncumbranceChanged;
        PlayerEncumbranceHost.ActiveChanged -= OnEncumbranceChanged;

        _body = null;
        _vitals = null;
        _stats = null;
        _moodEntries.Clear();
    }

    void OnBodyChanged()
    {
        if (_stats is DefaultPlayerStats dps)
            dps.Skills.Refresh();

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

    void OnEncumbranceChanged()
    {
        RebuildMoodEntries();
        MoodChanged?.Invoke();
        Changed?.Invoke();
    }

    void RebuildMoodEntries()
    {
        PlayerEncumbranceStage stage = PlayerEncumbranceHost.Active != null
            ? PlayerEncumbranceHost.Active.Stage
            : PlayerEncumbranceStage.None;
        PlayerStatusMoodEntries.Collect(_body, _vitals, stage, _moodEntries);
    }
}
