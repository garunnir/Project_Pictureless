// ============================================================
// PlayerStatusViewModel — Body/Vitals/Stats 바인드 + HUD 무드 수집
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public sealed class PlayerStatusViewModel
{
    ICharacterBody _body;
    IPlayerVitals _vitals;
    IPlayerStats _stats;
    PlayerNeedsHost _needs;
    CharacterMoodHost _mood;
    CharacterImbalanceHost _imbalance;
    ICharacterDefeat _defeat;

    readonly List<MoodEntry> _moodEntries = new(UIPlayerStatusSummaryPanel.MaxSlots);
    readonly List<MoodEntry> _moodSnapshot = new(UIPlayerStatusSummaryPanel.MaxSlots);

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

        if (_body != null)
            _body.Changed += OnBodyChanged;
        if (_vitals != null)
            _vitals.Changed += OnVitalsChanged;
        if (_stats != null)
            _stats.Changed += OnStatsChanged;

        PlayerEncumbranceHost.StageChanged += OnEncumbranceChanged;
        PlayerEncumbranceHost.ActiveChanged += OnEncumbranceChanged;
        BindNeeds(PlayerNeedsHost.Active);
        BindMood(CharacterMoodHost.Active);
        BindImbalance(CharacterImbalanceHost.Active);
        BindDefeat(GameplayData.Defeat);

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
        BindNeeds(null);
        BindMood(null);
        BindImbalance(null);
        BindDefeat(null);

        _body = null;
        _vitals = null;
        _stats = null;
        _moodEntries.Clear();
    }

    void RaiseMoodIfChanged()
    {
        if (RebuildMoodEntries())
            MoodChanged?.Invoke();
    }

    void OnBodyChanged()
    {
        RaiseMoodIfChanged();
        Changed?.Invoke();
    }

    void OnVitalsChanged(string _)
    {
        RaiseMoodIfChanged();
        Changed?.Invoke();
    }

    void OnStatsChanged(string _) => Changed?.Invoke();

    void OnEncumbranceChanged()
    {
        RaiseMoodIfChanged();
        Changed?.Invoke();
    }

    void OnNeedsChanged()
    {
        RaiseMoodIfChanged();
        Changed?.Invoke();
    }

    bool RebuildMoodEntries()
    {
        BindNeeds(PlayerNeedsHost.Active);
        BindMood(CharacterMoodHost.Active);
        BindImbalance(CharacterImbalanceHost.Active);
        BindDefeat(GameplayData.Defeat);
        PlayerEncumbranceStage stage = PlayerEncumbranceHost.Active != null
            ? PlayerEncumbranceHost.Active.Stage
            : PlayerEncumbranceStage.None;

        _moodSnapshot.Clear();
        for (int i = 0; i < _moodEntries.Count; i++)
            _moodSnapshot.Add(_moodEntries[i]);

        PlayerStatusMoodEntries.Collect(_body, _vitals, stage, _needs, _moodEntries);
        return !SameMoods(_moodSnapshot, _moodEntries);
    }

    static bool SameMoods(List<MoodEntry> a, List<MoodEntry> b)
    {
        if (a.Count != b.Count)
            return false;

        for (int i = 0; i < a.Count; i++)
        {
            MoodEntry left = a[i];
            MoodEntry right = b[i];
            if (left.IconId != right.IconId ||
                left.Polarity != right.Polarity ||
                left.TooltipText != right.TooltipText ||
                left.Intensity != right.Intensity)
            {
                return false;
            }
        }

        return true;
    }

    void BindNeeds(PlayerNeedsHost needs)
    {
        if (_needs == needs)
            return;

        if (_needs != null)
            _needs.Changed -= OnNeedsChanged;

        _needs = needs;
        if (_needs != null)
            _needs.Changed += OnNeedsChanged;
    }

    void BindMood(CharacterMoodHost mood)
    {
        if (_mood == mood)
            return;

        if (_mood != null)
            _mood.Changed -= OnMoodNeedChanged;

        _mood = mood;
        if (_mood != null)
            _mood.Changed += OnMoodNeedChanged;
    }

    void OnMoodNeedChanged()
    {
        RaiseMoodIfChanged();
        Changed?.Invoke();
    }

    void BindImbalance(CharacterImbalanceHost imbalance)
    {
        if (_imbalance == imbalance)
            return;

        if (_imbalance != null)
            _imbalance.Changed -= OnImbalanceChanged;

        _imbalance = imbalance;
        if (_imbalance != null)
            _imbalance.Changed += OnImbalanceChanged;
    }

    void OnImbalanceChanged()
    {
        RaiseMoodIfChanged();
        Changed?.Invoke();
    }

    void BindDefeat(ICharacterDefeat defeat)
    {
        if (_defeat == defeat)
            return;

        if (_defeat != null)
            _defeat.Changed -= OnDefeatChanged;

        _defeat = defeat;
        if (_defeat != null)
            _defeat.Changed += OnDefeatChanged;
    }

    void OnDefeatChanged()
    {
        RaiseMoodIfChanged();
        Changed?.Invoke();
    }
}
