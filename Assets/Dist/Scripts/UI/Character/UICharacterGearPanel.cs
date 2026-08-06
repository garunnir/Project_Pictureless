// ============================================================
// UICharacterGearPanel — 들기 L/R + 착용 목록 (Character 장비 탭)
// ============================================================

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UICharacterGearPanel : MonoBehaviour
{
    [SerializeField] RectTransform _wieldRoot;
    [SerializeField] RectTransform _wornRoot;
    [SerializeField] TMP_Text _hoverText;
    [SerializeField] TMP_Text _filterLabel;
    [SerializeField] TMP_Text _encTotalsText;

    UICharacterWieldSlotView _leftSlot;
    UICharacterWieldSlotView _rightSlot;
    readonly List<UICharacterWornRow> _rows = new(16);
    readonly List<ItemStack> _wornScratch = new(16);
    string _coverFilter;
    CharacterGearService _gear;
    PlayerGearHost _gearHost;
    int _strength;
    CharacterWindowTab _activeTab = CharacterWindowTab.Equipment;

    public void EnsureBuilt(RectTransform parent)
    {
        if (parent != null && transform.parent != parent)
            transform.SetParent(parent, false);

        WireExistingChildren();
        DestroyLegacyPanelProgress();

        if (_wieldRoot == null)
            _wieldRoot = CreateVertical("WieldRoot", transform);
        if (_wornRoot == null)
            _wornRoot = CreateVertical("WornRoot", transform);

        if (_hoverText == null)
        {
            GameObject hoverGo = new("HoverDetail");
            hoverGo.transform.SetParent(transform, false);
            RectTransform hoverRt = hoverGo.AddComponent<RectTransform>();
            hoverRt.sizeDelta = new Vector2(0f, 48f);
            _hoverText = hoverGo.AddComponent<TextMeshProUGUI>();
            _hoverText.fontSize = 14f;
            _hoverText.raycastTarget = false;
            ApplySharedFont(_hoverText);
        }

        if (_filterLabel == null)
        {
            Transform filterExisting = _wornRoot != null ? _wornRoot.Find("FilterLabel") : null;
            if (filterExisting != null)
                _filterLabel = filterExisting.GetComponent<TMP_Text>();
        }

        if (_filterLabel == null)
        {
            GameObject filterGo = new("FilterLabel");
            filterGo.transform.SetParent(_wornRoot, false);
            _filterLabel = filterGo.AddComponent<TextMeshProUGUI>();
            _filterLabel.fontSize = 16f;
            _filterLabel.text = CharacterGearLabels.WornFilterAll;
            _filterLabel.raycastTarget = false;
            ApplySharedFont(_filterLabel);
        }

        EnsureWieldSlots();
        EnsureEncTotals();
    }

    void WireExistingChildren()
    {
        if (_wieldRoot == null)
        {
            Transform t = transform.Find("WieldRoot");
            if (t != null)
                _wieldRoot = t as RectTransform;
        }

        if (_wornRoot == null)
        {
            Transform t = transform.Find("WornRoot");
            if (t != null)
                _wornRoot = t as RectTransform;
        }

        if (_hoverText == null)
        {
            Transform t = transform.Find("HoverDetail");
            if (t != null)
                _hoverText = t.GetComponent<TMP_Text>();
        }

        if (_encTotalsText == null)
        {
            Transform t = transform.Find("EncTotals");
            if (t != null)
                _encTotalsText = t.GetComponent<TMP_Text>();
        }

        if (_filterLabel == null && _wornRoot != null)
        {
            Transform t = _wornRoot.Find("FilterLabel");
            if (t != null)
                _filterLabel = t.GetComponent<TMP_Text>();
        }
    }

    void DestroyLegacyPanelProgress()
    {
        Transform progress = transform.Find("Progress");
        if (progress == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(progress.gameObject);
            return;
        }
#endif
        Destroy(progress.gameObject);
    }

    void EnsureWieldSlots()
    {
        if (_wieldRoot == null)
            return;

        if (_leftSlot == null)
        {
            Transform left = _wieldRoot.Find("Wield_L");
            if (left != null)
                _leftSlot = left.GetComponent<UICharacterWieldSlotView>();
            if (_leftSlot == null)
                _leftSlot = CreateSlot(_wieldRoot, WieldSlotId.Left);
        }

        if (_rightSlot == null)
        {
            Transform right = _wieldRoot.Find("Wield_R");
            if (right != null)
                _rightSlot = right.GetComponent<UICharacterWieldSlotView>();
            if (_rightSlot == null)
                _rightSlot = CreateSlot(_wieldRoot, WieldSlotId.Right);
        }
    }

    void EnsureEncTotals()
    {
        if (_encTotalsText != null)
            return;

        GameObject totalsGo = new("EncTotals");
        totalsGo.transform.SetParent(transform, false);
        _encTotalsText = totalsGo.AddComponent<TextMeshProUGUI>();
        _encTotalsText.fontSize = 14f;
        _encTotalsText.raycastTarget = false;
        _encTotalsText.gameObject.SetActive(false);
        ApplySharedFont(_encTotalsText);
    }

    public void SetActiveTab(CharacterWindowTab tab)
    {
        _activeTab = tab;
        Refresh();
    }

    public void ShowPartHover(string partId)
    {
        if (string.IsNullOrEmpty(partId) || _gear == null)
            return;

        WearStatsAggregator.WearPartArmorStats stats =
            WearStatsAggregator.ForPart(_gear.Wear, partId);
        string text = CharacterGearLabels.FormatPartArmorStats(partId, stats);
        PlayerGearHost host = PlayerGearHost.Active;
        WearEnvExposure exposure = host?.EnvExposure;
        if (exposure != null)
            text += "\n" + CharacterGearLabels.FormatWetnessLine(exposure);
        if (host != null)
            text += "\n" + CharacterGearLabels.FormatWeatherVisionLine(
                host.Weather,
                host.VisionFactor);
        ShowHover(text);
    }

    public void ShowBodyTempPartHover(string partId)
    {
        if (string.IsNullOrEmpty(partId) || _gear == null)
            return;

        WearStatsAggregator.WearPartArmorStats stats =
            WearStatsAggregator.ForPart(_gear.Wear, partId);
        string text = $"{partId}\n{CharacterGearLabels.HoverWarm} {stats.Warmth}";
        PlayerGearHost host = PlayerGearHost.Active;
        BodyTemp bodyTemp = host?.BodyTemperature;
        if (bodyTemp != null)
            text += "\n" + CharacterGearLabels.FormatBodyTempLine(bodyTemp);
        if (host != null)
            text += "\n" + CharacterGearLabels.FormatWeatherVisionLine(
                host.Weather,
                host.VisionFactor);
        ShowHover(text);
    }

    public void HidePartHover() => HideHover();

    public void Bind(CharacterGearService gear, int strength)
    {
        Unbind();
        _gear = gear;
        _strength = strength;
        if (_gear != null)
            _gear.Changed += Refresh;
        _gearHost = PlayerGearHost.Active;
        if (_gearHost != null)
            _gearHost.Changed += Refresh;
        Refresh();
    }

    public void Unbind()
    {
        if (_gear != null)
            _gear.Changed -= Refresh;
        _gear = null;
        if (_gearHost != null)
            _gearHost.Changed -= Refresh;
        _gearHost = null;
    }

    public void SetCoverFilter(string partId)
    {
        _coverFilter = partId;
        if (_filterLabel != null)
        {
            _filterLabel.text = string.IsNullOrEmpty(partId)
                ? CharacterGearLabels.WornFilterAll
                : partId;
        }

        Refresh();
    }

    public void ClearCoverFilter() => SetCoverFilter(null);

    public void Refresh()
    {
        if (_gear == null)
            return;

        EnsureEncTotals();
        EnsureWieldSlots();

        bool showWield = _activeTab == CharacterWindowTab.Equipment;
        if (_wieldRoot != null)
            _wieldRoot.gameObject.SetActive(showWield);

        if (showWield)
        {
            _leftSlot?.Bind(_gear, WieldSlotId.Left, _strength, ShowHover, HideHover, OnSlotUnequip);
            _rightSlot?.Bind(_gear, WieldSlotId.Right, _strength, ShowHover, HideHover, OnSlotUnequip);
        }

        _gear.Wear.CollectFiltered(_coverFilter, _wornScratch);
        while (_rows.Count < _wornScratch.Count)
            _rows.Add(CreateWornRow(_wornRoot));

        for (int i = 0; i < _rows.Count; i++)
        {
            if (i < _wornScratch.Count)
            {
                _rows[i].gameObject.SetActive(true);
                _rows[i].Bind(_wornScratch[i], _gear, _strength, ShowHover, HideHover, OnWornUnequip);
            }
            else
            {
                _rows[i].gameObject.SetActive(false);
            }
        }

        bool encTab = _activeTab == CharacterWindowTab.Encumbrance;
        bool bodyTempTab = _activeTab == CharacterWindowTab.BodyTemp;
        if (_encTotalsText != null)
        {
            _encTotalsText.gameObject.SetActive(encTab || bodyTempTab);
            if (encTab)
            {
                WearStatsAggregator.WearArmorTotals totals =
                    WearStatsAggregator.Aggregate(_gear.Wear);
                PlayerGearHost host = PlayerGearHost.Active;
                _encTotalsText.text = CharacterGearLabels.FormatEncTotalsWithWetness(
                    totals,
                    host?.EnvExposure,
                    host?.Weather,
                    host != null ? host.VisionFactor : HelmetVision.FullVisionFactor);
            }
            else if (bodyTempTab)
            {
                WearStatsAggregator.WearArmorTotals totals =
                    WearStatsAggregator.Aggregate(_gear.Wear);
                PlayerGearHost host = PlayerGearHost.Active;
                _encTotalsText.text = CharacterGearLabels.FormatBodyTempTotals(
                    totals,
                    host?.BodyTemperature,
                    host?.Weather,
                    host != null ? host.VisionFactor : HelmetVision.FullVisionFactor);
            }
        }
    }

    void Update()
    {
        if (_gear == null || !_gear.IsBusy)
            return;

        RefreshNameBars();
    }

    void RefreshNameBars()
    {
        _leftSlot?.RefreshNameBar();
        _rightSlot?.RefreshNameBar();
        for (int i = 0; i < _rows.Count; i++)
        {
            if (_rows[i] != null && _rows[i].gameObject.activeSelf)
                _rows[i].RefreshNameBar();
        }
    }

    void OnDestroy() => Unbind();

    void ShowHover(string text)
    {
        if (_hoverText != null)
            _hoverText.text = text ?? string.Empty;
    }

    void HideHover()
    {
        if (_hoverText != null)
            _hoverText.text = string.Empty;
    }

    void OnSlotUnequip(WieldSlotId slot, bool toFloor)
    {
        _gear?.TryBeginUnwieldSlot(slot, toFloor);
    }

    void OnWornUnequip(ItemStack stack, bool toFloor)
    {
        _gear?.TryBeginTakeOff(stack, toFloor);
    }

    void ApplySharedFont(TMP_Text target)
    {
        if (target == null)
            return;

        UICharacterWindow window = GetComponentInParent<UICharacterWindow>();
        if (window == null)
            return;

        TMP_Text title = window.GetComponentInChildren<TMP_Text>(true);
        // Prefer Title under Header when present.
        Transform header = window.transform.Find("Header/Title");
        if (header != null)
        {
            TMP_Text headerTitle = header.GetComponent<TMP_Text>();
            if (headerTitle != null && headerTitle.font != null)
            {
                target.font = headerTitle.font;
                return;
            }
        }

        if (title != null && title.font != null)
            target.font = title.font;
    }

    static RectTransform CreateVertical(string name, Transform parent)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.spacing = 4f;
        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return go.transform as RectTransform;
    }

    static UICharacterWieldSlotView CreateSlot(Transform parent, WieldSlotId slot)
    {
        GameObject go = new(slot == WieldSlotId.Left ? "Wield_L" : "Wield_R");
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 36f;
        le.preferredHeight = 36f;
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.18f, 0.9f);
        bg.raycastTarget = true;
        return go.AddComponent<UICharacterWieldSlotView>();
    }

    static UICharacterWornRow CreateWornRow(Transform parent)
    {
        GameObject go = new("WornRow");
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 28f;
        le.preferredHeight = 28f;
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.16f, 0.16f, 0.5f);
        bg.raycastTarget = true;
        return go.AddComponent<UICharacterWornRow>();
    }
}
