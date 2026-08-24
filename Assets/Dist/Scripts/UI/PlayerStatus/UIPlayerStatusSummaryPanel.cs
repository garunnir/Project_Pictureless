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
    public const string ConsciousnessFillPath =
        "Area_Status/Grp_Body/BodyGrapicSet/Img_Layer1BodyOutline_consciousness_Fill";
    public const string BloodFillPath =
        "Area_Status/Grp_Body/BodyGrapicSet/Img_Layer1BodyOutline_blood_Fill";
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
    [SerializeField] Image _consciousnessFill;
    [SerializeField] Image _bloodFill;

    readonly List<UIPlayerStatusMoodIconSlot> _slots = new(MaxSlots);
    readonly Dictionary<MoodIconId, float> _lastIntensityByIcon = new(MaxSlots);
    readonly List<UIPlayerStatusBodyPartGraphic> _graphics = new(16);

    PlayerStatusViewModel _viewModel;
    Canvas _rootCanvas;
    CharacterWindowTab _tab = CharacterWindowTab.Status;
    UIPlayerStatusMoodIconSlot _hoveredSlot;

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

        BringSlotRootToFront();
    }

    void OnDisable() => HideTooltip();

    public void SetBodyTab(CharacterWindowTab tab)
    {
        _tab = tab;
        _bodyTabStrip?.SetSelectedTab(tab);
        RefreshBody();
    }

    public void RefreshBody()
    {
        RefreshCapacityFills();
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

    void RefreshCapacityFills()
    {
        EnsureCapacityFills();
        ICharacterBody body = _viewModel != null ? _viewModel.Body : null;
        if (_consciousnessFill != null)
            _consciousnessFill.fillAmount = body != null ? BodyCapacity.Consciousness(body) : 0f;
        if (_bloodFill != null)
            _bloodFill.fillAmount = body != null ? body.Blood01 : 0f;
    }

    void EnsureCapacityFills()
    {
        if (_consciousnessFill == null)
            _consciousnessFill = FindFill(ConsciousnessFillPath);
        if (_bloodFill == null)
            _bloodFill = FindFill(BloodFillPath);
    }

    Image FindFill(string path)
    {
        Transform found = transform.Find(path);
        return found != null ? found.GetComponent<Image>() : null;
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
        {
            UIPlayerStatusBodyPartGraphic graphic = found[i];
            _graphics.Add(graphic);
            if (graphic != null && !string.IsNullOrEmpty(graphic.PartId))
                graphic.Bind(graphic.PartId, onHover: null, onExit: null, onClick: null, OnPartRightClick);
        }
    }

    void OnPartRightClick(string partId, Vector2 screenPosition)
    {
        if (_tab != CharacterWindowTab.Status)
            return;
        BodyPartHealContextMenuBuilder.TryShow(partId, screenPosition);
    }

    public void Refresh()
    {
        if (_viewModel == null)
            return;

        IReadOnlyList<MoodEntry> entries = _viewModel.MoodEntries;

        EnsureSlots(entries.Count);

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
        SyncHoveredTooltip();
        RefreshCapacityFills();
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

    void EnsureSlots(int needed)
    {
        if (_slotRoot == null || _slotPrefab == null)
            return;

        if (_slotPrefab.gameObject.activeSelf)
            _slotPrefab.gameObject.SetActive(false);

        while (_slots.Count < needed)
        {
            UIPlayerStatusMoodIconSlot slot = Instantiate(_slotPrefab, _slotRoot);
            slot.Initialize(this);
            _slots.Add(slot);
        }

        BringSlotRootToFront();
    }

    void BringSlotRootToFront()
    {
        if (_slotRoot != null)
            _slotRoot.SetAsLastSibling();
    }

    public void ShowTooltip(UIPlayerStatusMoodIconSlot slot)
    {
        if (slot == null)
            return;

        _hoveredSlot = slot;
        PresentTooltip(slot.TooltipText, slot.transform as RectTransform);
    }

    public void HideTooltip(UIPlayerStatusMoodIconSlot slot)
    {
        if (slot != null && _hoveredSlot != slot)
            return;

        HideTooltip();
    }

    public void HideTooltip()
    {
        _hoveredSlot = null;
        if (_tooltipShell != null)
            _tooltipShell.Hide();
        else if (_tooltipRoot != null)
            _tooltipRoot.gameObject.SetActive(false);
    }

    void SyncHoveredTooltip()
    {
        if (_hoveredSlot == null)
            return;

        if (!_hoveredSlot.isActiveAndEnabled || string.IsNullOrEmpty(_hoveredSlot.TooltipText))
        {
            HideTooltip();
            return;
        }

        PresentTooltip(_hoveredSlot.TooltipText, _hoveredSlot.transform as RectTransform);
    }

    void PresentTooltip(string text, RectTransform anchor)
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
