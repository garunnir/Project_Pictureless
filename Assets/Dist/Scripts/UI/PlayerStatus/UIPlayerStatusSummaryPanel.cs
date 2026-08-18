// ============================================================
// UIPlayerStatusSummaryPanel — 상태 요약 HUD (무드 스트립 + 피격도)
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIPlayerStatusSummaryPanel : MonoBehaviour
{
    public const int MaxSlots = 8;
    const string BodyPartsPath = "Area_Status/Grp_Body/Parts";
    const string SwitchPath = "Area_Status/Grp_Switch";

    static readonly UIHoverStyle MoodHoverStyle = new(new Vector2(0f, 28f), followMouse: false);

    [SerializeField] RectTransform _slotRoot;
    [SerializeField] UIPlayerStatusMoodIconSlot _slotPrefab;
    [SerializeField] PlayerStatusMoodIconCatalog _iconCatalog;
    [SerializeField] RectTransform _tooltipRoot;
    [SerializeField] TMP_Text _tooltipText;
    [SerializeField] UIHoverPanelShell _tooltipShell;
    [SerializeField] RectTransform _bodyPartsRoot;
    [SerializeField] UIPlayerStatusBodyTabStrip _bodyTabStrip;

    readonly List<UIPlayerStatusMoodIconSlot> _slots = new(MaxSlots);
    readonly Dictionary<MoodIconId, float> _lastIntensityByIcon = new(MaxSlots);
    readonly List<UIPlayerStatusBodyPartGraphic> _graphics = new(16);

    PlayerStatusViewModel _viewModel;
    Canvas _rootCanvas;
    CharacterWindowTab _tab = CharacterWindowTab.Status;

    public CharacterWindowTab ActiveTab => _tab;
    public event Action<CharacterWindowTab> BodyTabChanged;

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
        _tooltipShell = tooltipRoot != null ? tooltipRoot.GetComponent<UIHoverPanelShell>() : null;
        _rootCanvas = null;
    }

    public void BindViewModel(PlayerStatusViewModel viewModel) => _viewModel = viewModel;

    void OnEnable()
    {
        EnsureBodyTabStrip();
        if (_bodyTabStrip != null)
        {
            _bodyTabStrip.Initialize(OnBodyTabStripSelected);
            _bodyTabStrip.SetSelectedTab(_tab);
        }
    }

    public void SetBodyTab(CharacterWindowTab tab)
    {
        _tab = tab;
        _bodyTabStrip?.SetSelectedTab(tab);
        RefreshBody();
    }

    public void RefreshBody()
    {
        EnsureBodyGraphics();
        if (_graphics.Count == 0)
            return;

        PlayerStatusBodyGraphicDisplay.Apply(_graphics, _viewModel?.Body, _tab);
    }

    void OnBodyTabStripSelected(CharacterWindowTab tab)
    {
        SetBodyTab(tab);
        BodyTabChanged?.Invoke(tab);
    }

    void EnsureBodyTabStrip()
    {
        if (_bodyTabStrip != null)
            return;

        Transform switchT = transform.Find(SwitchPath);
        if (switchT != null)
            _bodyTabStrip = switchT.GetComponent<UIPlayerStatusBodyTabStrip>();
    }

    void EnsureBodyGraphics()
    {
        if (_graphics.Count > 0)
            return;

        if (_bodyPartsRoot == null)
            _bodyPartsRoot = transform.Find(BodyPartsPath) as RectTransform;

        if (_bodyPartsRoot == null)
        {
            Debug.LogError(
                "[UIPlayerStatusSummaryPanel] Grp_Body/Parts missing on summary HUD prefab.",
                this);
            return;
        }

        UIPlayerStatusBodyPartGraphic[] found =
            _bodyPartsRoot.GetComponentsInChildren<UIPlayerStatusBodyPartGraphic>(true);
        if (found.Length == 0)
        {
            Debug.LogError(
                "[UIPlayerStatusSummaryPanel] UIPlayerStatusBodyPartGraphic missing on HUD Parts. " +
                "Run Dist/MCP/PlayerStatus/Patch Summary Body Hits.",
                _bodyPartsRoot);
            return;
        }

        for (int i = 0; i < found.Length; i++)
            _graphics.Add(found[i]);
    }

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

        EnsureTooltipHoverLayout();
        UIHoverCanvasLayer.EnsureParent(_tooltipRoot, _rootCanvas);
        UIHoverCanvasLayer.BringToFront(_tooltipRoot);
        _tooltipShell.ShowNearAnchor(anchor, MoodHoverStyle);
    }

    public void HideTooltip()
    {
        if (_tooltipShell != null)
            _tooltipShell.Hide();
        else if (_tooltipRoot != null)
            _tooltipRoot.gameObject.SetActive(false);
    }

    void EnsureTooltipHoverLayout()
    {
        // Positioner sets anchoredPosition in parent local space — center anchors required.
        _tooltipRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _tooltipRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _tooltipRoot.pivot = new Vector2(0.5f, 0f);
    }

    void EnsureTooltipShell()
    {
        if (_tooltipRoot == null)
            return;

        if (_rootCanvas == null)
            _rootCanvas = _tooltipRoot.GetComponentInParent<Canvas>();

        if (_tooltipShell == null)
            _tooltipShell = _tooltipRoot.GetComponent<UIHoverPanelShell>();

        if (_tooltipShell == null)
        {
            Debug.LogError(
                "[UIPlayerStatusSummaryPanel] UIHoverPanelShell missing on Tooltip. Bake onto Grp_PlayerStatusSummary prefab.",
                _tooltipRoot);
            return;
        }

        if (_rootCanvas != null)
            _tooltipShell.Initialize(_rootCanvas);
    }
}
