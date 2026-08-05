// ============================================================
// UIPlayerStatusSummaryPanel — 상태 요약 HUD 아이콘 스트립 + 툴팁
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIPlayerStatusSummaryPanel : MonoBehaviour
{
    public const int MaxSlots = 8;

    static readonly UIHoverStyle MoodHoverStyle = new(new Vector2(0f, 28f), followMouse: false);

    [SerializeField] RectTransform _slotRoot;
    [SerializeField] UIPlayerStatusMoodIconSlot _slotPrefab;
    [SerializeField] PlayerStatusMoodIconCatalog _iconCatalog;
    [SerializeField] RectTransform _tooltipRoot;
    [SerializeField] TMP_Text _tooltipText;
    [SerializeField] UIHoverPanelShell _tooltipShell;

    readonly List<UIPlayerStatusMoodIconSlot> _slots = new(MaxSlots);
    readonly Dictionary<MoodIconId, float> _lastIntensityByIcon = new(MaxSlots);

    PlayerStatusViewModel _viewModel;
    Canvas _rootCanvas;

    public void Wire(
        RectTransform slotRoot,
        UIPlayerStatusMoodIconSlot slotPrefab,
        PlayerStatusMoodIconCatalog iconCatalog,
        RectTransform tooltipRoot,
        TMP_Text tooltipText)
    {
        _slotRoot = slotRoot;
        _slotPrefab = slotPrefab;
        _iconCatalog = iconCatalog;
        _tooltipRoot = tooltipRoot;
        _tooltipText = tooltipText;
        _tooltipShell = null;
        _rootCanvas = null;
    }

    public void BindViewModel(PlayerStatusViewModel viewModel) => _viewModel = viewModel;

    public void Refresh()
    {
        if (_viewModel == null)
            return;

        IReadOnlyList<MoodEntry> entries = _viewModel.MoodEntries;
        HideTooltip();

        EnsureSlots();

        for (int i = 0; i < _slots.Count; i++)
        {
            UIPlayerStatusMoodIconSlot slot = _slots[i];
            if (i >= entries.Count)
            {
                slot.SetVisible(false);
                continue;
            }

            MoodEntry entry = entries[i];
            Sprite front = null;
            if (_iconCatalog != null)
            {
                if (!_iconCatalog.TryGetFront(entry.IconId, out front) &&
                    entry.IconId != MoodIconId.Discomfort)
                {
                    _iconCatalog.TryGetFront(MoodIconId.Discomfort, out front);
                }
            }

            slot.Apply(entry, front);
            slot.SetVisible(true);

            if (ShouldAttentionShake(entry))
                slot.PlayAttentionShake();
        }

        UpdateIntensitySnapshot(entries);
        gameObject.SetActive(entries.Count > 0);
    }

    bool ShouldAttentionShake(MoodEntry entry)
    {
        if (!_lastIntensityByIcon.TryGetValue(entry.IconId, out float previousIntensity))
            return true;

        return !Mathf.Approximately(previousIntensity, entry.Intensity);
    }

    void UpdateIntensitySnapshot(IReadOnlyList<MoodEntry> entries)
    {
        _lastIntensityByIcon.Clear();
        for (int i = 0; i < entries.Count; i++)
        {
            MoodEntry entry = entries[i];
            _lastIntensityByIcon[entry.IconId] = entry.Intensity;
        }
    }

    void EnsureSlots()
    {
        if (_slotRoot == null || _slotPrefab == null)
            return;

        while (_slots.Count < MaxSlots)
        {
            UIPlayerStatusMoodIconSlot slot = Instantiate(_slotPrefab, _slotRoot);
            slot.Initialize(this);
            _slots.Add(slot);
        }
    }

    public void ShowTooltip(string text, RectTransform anchor)
    {
        if (_tooltipRoot == null || _tooltipText == null || string.IsNullOrEmpty(text))
            return;

        _tooltipText.text = text;
        EnsureTooltipShell();
        if (_tooltipShell == null)
            return;

        _tooltipShell.ShowNearAnchor(anchor, MoodHoverStyle);
    }

    public void HideTooltip()
    {
        if (_tooltipShell != null)
            _tooltipShell.Hide();
        else if (_tooltipRoot != null)
            _tooltipRoot.gameObject.SetActive(false);
    }

    void EnsureTooltipShell()
    {
        if (_tooltipRoot == null)
            return;

        if (_rootCanvas == null)
            _rootCanvas = _tooltipRoot.GetComponentInParent<Canvas>();

        if (_tooltipShell == null)
        {
            _tooltipShell = _tooltipRoot.GetComponent<UIHoverPanelShell>();
            if (_tooltipShell == null)
                _tooltipShell = _tooltipRoot.gameObject.AddComponent<UIHoverPanelShell>();

            if (_rootCanvas != null)
                _tooltipShell.Initialize(_rootCanvas);
        }
    }
}
