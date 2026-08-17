// ============================================================
// UICharacterWindow — Character 창 (상태|장비|방해|체온) + Status 패리티
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UICharacterWindow : MonoBehaviour
{
    [SerializeField] TMP_Text _headerTitle;
    [SerializeField] RectTransform _bodyPartViewsRoot;
    [SerializeField] TMP_Text _vitalsText;
    [SerializeField] TMP_Text _skillsText;
    [SerializeField] Button _debugSeverArmLButton;
    [SerializeField] TMP_Text _debugSeverArmLLabel;
    [SerializeField] UIPlayerStatusDetailPanel _detailPanel;
    [SerializeField] UIWindowDragHandler _windowDragHandler;
    [SerializeField] RectTransform _statusContentRoot;
    [SerializeField] RectTransform _bodyStatusRoot;
    [SerializeField] RectTransform _tabBarRoot;
    [SerializeField] RectTransform _gearPanelRoot;
    [SerializeField] UICharacterGearPanel _gearPanel;

    readonly List<UIPlayerStatusBodyPartGraphic> _graphics = new(16);
    readonly List<UIPlayerStatusBodyPartRow> _rows = new(16);
    readonly List<Button> _tabButtons = new(4);

    PlayerStatusViewModel _viewModel;
    CharacterWindowTab _tab = CharacterWindowTab.Status;
    Action _onChromeClose;

    public bool IsVisible => gameObject.activeSelf;
    public RectTransform WindowRect => transform as RectTransform;
    public CharacterWindowTab ActiveTab => _tab;

    public void BindChromeClose(Action onClose) => _onChromeClose = onClose;

    public void ConfigureChrome(Canvas rootCanvas)
    {
        if (_windowDragHandler == null)
            Debug.LogError("[UICharacterWindow] Window drag handler not assigned.", this);

        _windowDragHandler?.Initialize(WindowRect, rootCanvas);

        Vector2 minSize = new(PlayerStatusWindowLayout.MinWidth, PlayerStatusWindowLayout.MinHeight);
        Vector2 maxSize = PlayerStatusWindowLayout.GetMaxSize(rootCanvas);

        UIWindowResizeHandles resizeHandles = GetComponent<UIWindowResizeHandles>();
        if (resizeHandles == null)
            Debug.LogError(
                "[UICharacterWindow] UIWindowResizeHandles missing on window root.",
                this);
        else
            resizeHandles.Initialize(WindowRect, rootCanvas, minSize, maxSize);

        if (WindowRect != null && rootCanvas != null)
            WindowRect.sizeDelta = PlayerStatusWindowLayout.ClampSize(WindowRect.sizeDelta, rootCanvas);

        if (!TryGetComponent(out UIOverlayWindow _))
            Debug.LogError("[UICharacterWindow] UIOverlayWindow missing on window prefab root.", this);

        UIWindowChromeBar.BindCloseOnWindow(this, _onChromeClose);

        _detailPanel?.Initialize(rootCanvas);
        EnsureTabChrome();
        EnsureGearPanel();
    }

    public void SetHeaderTitle(string title)
    {
        if (_headerTitle != null)
            _headerTitle.text = title;
    }

    public void Initialize(PlayerStatusViewModel viewModel)
    {
        Unbind();
        _viewModel = viewModel;

        if (_viewModel != null)
            _viewModel.Changed += Refresh;

        bool debugControlsEnabled = Debug.isDebugBuild;
        if (_debugSeverArmLButton != null)
        {
            _debugSeverArmLButton.gameObject.SetActive(debugControlsEnabled);
            _debugSeverArmLButton.onClick.RemoveListener(OnDebugSeverArmL);
            if (debugControlsEnabled)
                _debugSeverArmLButton.onClick.AddListener(OnDebugSeverArmL);
        }

        if (_debugSeverArmLLabel != null && debugControlsEnabled)
            _debugSeverArmLLabel.text = PlayerStatusLabels.DebugSeverArmL;

        EnsureTabChrome();
        EnsureGearPanel();
        EnsurePartViews();
        BindGear();
        SetTab(CharacterWindowTab.Status);
        Refresh();
    }

    public void Unbind()
    {
        if (_viewModel != null)
            _viewModel.Changed -= Refresh;
        if (_debugSeverArmLButton != null)
            _debugSeverArmLButton.onClick.RemoveListener(OnDebugSeverArmL);

        _gearPanel?.Unbind();
        _viewModel = null;
        _detailPanel?.Hide();
    }

    public void SetTab(CharacterWindowTab tab)
    {
        _tab = tab;
        ApplyTabVisibility();
        _gearPanel?.SetActiveTab(tab);
        Refresh();
    }

    void OnDestroy() => Unbind();

    void OnDebugSeverArmL()
    {
        _viewModel?.Body?.RemovePart(BodyPartIds.UpperArmL);
    }

    void EnsureTabChrome()
    {
        if (_tabBarRoot == null)
        {
            Transform existing = transform.Find("TabBar");
            if (existing != null)
                _tabBarRoot = existing as RectTransform;
        }

        if (_tabBarRoot == null)
        {
            Debug.LogWarning(
                "[UICharacterWindow] TabBar missing on prefab — creating fallback chrome.",
                this);
            GameObject bar = new("TabBar");
            bar.transform.SetParent(transform, false);
            _tabBarRoot = bar.AddComponent<RectTransform>();
            _tabBarRoot.anchorMin = new Vector2(0f, 1f);
            _tabBarRoot.anchorMax = new Vector2(1f, 1f);
            _tabBarRoot.pivot = new Vector2(0.5f, 1f);
            _tabBarRoot.sizeDelta = new Vector2(0f, 28f);
            _tabBarRoot.anchoredPosition = new Vector2(0f, -40f);
            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.spacing = 4f;
            layout.padding = new RectOffset(8, 8, 2, 2);
        }

        if (_statusContentRoot == null)
        {
            Transform content = transform.Find("Area_Content");
            if (content != null)
                _statusContentRoot = content as RectTransform;
            else
                _statusContentRoot = _bodyPartViewsRoot;
        }

        BindOrCreateTabButtons();
    }

    void BindOrCreateTabButtons()
    {
        if (_tabButtons.Count > 0)
            return;

        BindTabButton(CharacterWindowTab.Status, CharacterGearLabels.TabStatus);
        BindTabButton(CharacterWindowTab.Equipment, CharacterGearLabels.TabEquipment);
        BindTabButton(CharacterWindowTab.Encumbrance, CharacterGearLabels.TabEncumbrance);
        BindTabButton(CharacterWindowTab.BodyTemp, CharacterGearLabels.TabBodyTemp);
    }

    void BindTabButton(CharacterWindowTab tab, string label)
    {
        string childName = "Tab_" + tab;
        Transform existing = _tabBarRoot != null ? _tabBarRoot.Find(childName) : null;
        Button button = existing != null ? existing.GetComponent<Button>() : null;
        if (button == null)
            button = CreateTabButton(tab, label);

        CharacterWindowTab captured = tab;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SetTab(captured));
        _tabButtons.Add(button);
    }

    Button CreateTabButton(CharacterWindowTab tab, string label)
    {
        GameObject go = new($"Tab_{tab}");
        go.transform.SetParent(_tabBarRoot, false);
        go.AddComponent<LayoutElement>().flexibleWidth = 1f;

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.22f, 0.22f, 0.22f, 1f);
        bg.raycastTarget = true;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = bg;

        GameObject labelGo = new("Label");
        labelGo.transform.SetParent(go.transform, false);
        RectTransform labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        TextMeshProUGUI text = labelGo.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = GearConstants.UiFontSizeTab;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        DistUiFont.Apply(text);

        return button;
    }

    void EnsureGearPanel()
    {
        if (_gearPanelRoot == null)
        {
            Transform existing = transform.Find("GearPanelRoot");
            if (existing != null)
                _gearPanelRoot = existing as RectTransform;
        }

        if (_gearPanelRoot == null)
        {
            Debug.LogWarning(
                "[UICharacterWindow] GearPanelRoot missing on prefab — creating fallback chrome.",
                this);
            GameObject root = new("GearPanelRoot");
            root.transform.SetParent(transform, false);
            _gearPanelRoot = root.AddComponent<RectTransform>();
            _gearPanelRoot.anchorMin = new Vector2(0.48f, 0f);
            _gearPanelRoot.anchorMax = new Vector2(1f, 1f);
            _gearPanelRoot.offsetMin = new Vector2(4f, 8f);
            _gearPanelRoot.offsetMax = new Vector2(-8f, -68f);
            Image panelBg = root.AddComponent<Image>();
            panelBg.color = new Color(0.14f, 0.14f, 0.14f, 0.92f);
            panelBg.raycastTarget = false;
        }

        if (_gearPanel == null)
            _gearPanel = _gearPanelRoot.GetComponent<UICharacterGearPanel>();
        if (_gearPanel == null)
            _gearPanel = _gearPanelRoot.gameObject.AddComponent<UICharacterGearPanel>();

        _gearPanel.EnsureBuilt(_gearPanelRoot);
        _gearPanel.SetHoverHandlers(OnGearItemHover, OnGearItemHoverExit);
    }

    void BindGear()
    {
        CharacterGearService gear = PlayerGearHost.Active?.Service;
        int str = 0;
        IPlayerStats stats = _viewModel?.Stats;
        if (stats != null)
            str = stats.GetSkillLevel(AttributeIds.Str);
        else if (GameplayData.CharacterSkills != null)
            str = GameplayData.CharacterSkills.Level(AttributeIds.Str);

        _gearPanel?.SetHoverHandlers(OnGearItemHover, OnGearItemHoverExit);
        _gearPanel?.Bind(gear, str);
    }

    void OnGearItemHover(string text, RectTransform anchor)
    {
        if (_detailPanel == null || string.IsNullOrEmpty(text))
            return;
        RectTransform a = anchor != null ? anchor : _gearPanelRoot;
        _detailPanel.ShowText(text, a);
    }

    void OnGearItemHoverExit()
    {
        _detailPanel?.Hide();
    }

    void ApplyTabVisibility()
    {
        bool statusLike = _tab == CharacterWindowTab.Status;
        bool gearLike = _tab == CharacterWindowTab.Equipment
            || _tab == CharacterWindowTab.Encumbrance
            || _tab == CharacterWindowTab.BodyTemp;

        if (_bodyStatusRoot == null)
        {
            Transform bodyStatus = transform.Find("Area_BodyProfile/Area_BodyStatus");
            if (bodyStatus != null)
                _bodyStatusRoot = bodyStatus as RectTransform;
        }

        if (_statusContentRoot != null)
            _statusContentRoot.gameObject.SetActive(statusLike);
        if (_bodyStatusRoot != null)
            _bodyStatusRoot.gameObject.SetActive(statusLike);
        if (_vitalsText != null)
            _vitalsText.gameObject.SetActive(statusLike);
        if (_skillsText != null)
            _skillsText.gameObject.SetActive(statusLike);
        if (_gearPanelRoot != null)
            _gearPanelRoot.gameObject.SetActive(gearLike);

        if (_tab == CharacterWindowTab.Status)
            _gearPanel?.ClearCoverFilter();
    }

    void ApplySharedFont(TMP_Text target) => DistUiFont.Apply(target);

    void EnsurePartViews()
    {
        if (_bodyPartViewsRoot == null)
            return;

        if (_graphics.Count == 0)
        {
            UIPlayerStatusBodyPartGraphic[] existingGraphics =
                _bodyPartViewsRoot.GetComponentsInChildren<UIPlayerStatusBodyPartGraphic>(true);
            for (int i = 0; i < existingGraphics.Length; i++)
                _graphics.Add(existingGraphics[i]);
        }

        string[] mains = BodyPartIds.MainConditionParts;
        if (_graphics.Count > 0)
        {
            for (int i = 0; i < _graphics.Count; i++)
            {
                string partId = _graphics[i].PartId;
                if (string.IsNullOrEmpty(partId))
                    continue;

                _graphics[i].Bind(partId, OnPartHover, OnPartExit, OnPartClick);
            }
            return;
        }

        if (_rows.Count == 0)
        {
            UIPlayerStatusBodyPartRow[] existing =
                _bodyPartViewsRoot.GetComponentsInChildren<UIPlayerStatusBodyPartRow>(true);
            for (int i = 0; i < existing.Length; i++)
                _rows.Add(existing[i]);
        }

        while (_rows.Count < mains.Length)
        {
            UIPlayerStatusBodyPartRow row =
                PlayerStatusUIFactory.CreateBodyPartRow(_bodyPartViewsRoot);
            _rows.Add(row);
        }

        for (int i = 0; i < mains.Length && i < _rows.Count; i++)
            _rows[i].Bind(mains[i], OnPartHover, OnPartExit);
    }

    void OnPartHover(string partId, RectTransform anchor)
    {
        if (_tab == CharacterWindowTab.Encumbrance)
        {
            _gearPanel?.ShowPartHover(partId, anchor);
            return;
        }

        if (_tab == CharacterWindowTab.BodyTemp)
        {
            _gearPanel?.ShowBodyTempPartHover(partId, anchor);
            return;
        }

        ICharacterBody body = _viewModel?.Body;
        if (_detailPanel == null || body == null || _tab != CharacterWindowTab.Status)
            return;

        _detailPanel.ShowForPart(body, partId, anchor);
    }

    void OnPartClick(string partId)
    {
        if (_tab != CharacterWindowTab.Equipment
            && _tab != CharacterWindowTab.Encumbrance
            && _tab != CharacterWindowTab.BodyTemp)
            return;

        if (_gearPanel == null)
            return;

        // Toggle: same part click clears filter (전체).
        if (string.Equals(_gearPanel.CoverFilter, partId, StringComparison.Ordinal))
            _gearPanel.ClearCoverFilter();
        else
            _gearPanel.SetCoverFilter(partId);
    }

    void OnPartExit()
    {
        _detailPanel?.Hide();
        if (_tab == CharacterWindowTab.Encumbrance || _tab == CharacterWindowTab.BodyTemp)
            _gearPanel?.HidePartHover();
    }

    public void Refresh()
    {
        if (_viewModel == null)
            return;

        SetHeaderTitle(CharacterGearLabels.Title);
        EnsurePartViews();

        ICharacterBody body = _viewModel.Body;
        string[] mains = BodyPartIds.MainConditionParts;
        for (int i = 0; i < _graphics.Count; i++)
        {
            string partId = _graphics[i].PartId;
            if (string.IsNullOrEmpty(partId))
                continue;

            bool present = body != null && body.Has(partId);
            int cur = present ? body.GetConditionCur(partId) : 0;
            int max = present ? body.GetConditionMax(partId) : 0;

            if (_tab == CharacterWindowTab.Encumbrance)
            {
                int enc = WearStatsAggregator.EncumbranceForPart(
                    PlayerGearHost.Active?.Wear, partId);
                _graphics[i].SetDisplay(enc, Mathf.Max(enc, 1), present);
            }
            else if (_tab == CharacterWindowTab.BodyTemp)
            {
                int warm = WearStatsAggregator.WarmthForPart(
                    PlayerGearHost.Active?.Wear, partId);
                _graphics[i].SetDisplay(warm, Mathf.Max(warm, 1), present);
            }
            else
            {
                _graphics[i].SetDisplay(cur, max, present);
            }
        }

        for (int i = 0; i < _rows.Count && i < mains.Length; i++)
        {
            string partId = mains[i];
            bool present = body != null && body.Has(partId);
            int cur = present ? body.GetConditionCur(partId) : 0;
            int max = present ? body.GetConditionMax(partId) : 0;
            _rows[i].SetDisplay(PlayerStatusLabels.GetPartName(partId), cur, max, present);
        }

        if (_tab == CharacterWindowTab.Status)
        {
            RefreshVitals();
            RefreshSkills();
        }

        _gearPanel?.Refresh();
    }

    void RefreshVitals()
    {
        if (_vitalsText == null)
            return;

        IPlayerVitals vitals = _viewModel?.Vitals;
        if (vitals == null)
        {
            _vitalsText.text = string.Empty;
            return;
        }

        bool showNumeric = _viewModel.CanShowNumericVitals;
        var lines = new List<string>(VitalKeys.All.Length + 1)
        {
            PlayerStatusLabels.VitalsSection
        };

        for (int i = 0; i < VitalKeys.All.Length; i++)
        {
            string key = VitalKeys.All[i];
            int cur = vitals.GetCurrent(key);
            int max = vitals.GetMax(key);

            if (showNumeric)
            {
                lines.Add(
                    $"{PlayerStatusLabels.GetVitalName(key)}  " +
                    PlayerStatusLabels.FormatVital(cur, max));
            }
            else
            {
                lines.Add(PlayerStatusLabels.FormatVitalProse(key, cur, max));
            }
        }

        _vitalsText.text = string.Join("\n", lines);
    }

    void RefreshSkills()
    {
        if (_skillsText == null)
            return;

        IPlayerStats stats = _viewModel?.Stats;
        if (stats == null)
        {
            _skillsText.text = string.Empty;
            return;
        }

        var lines = new List<string> { PlayerStatusLabels.SkillsSection };
        IReadOnlyCollection<string> skills = stats.GetKnownSkillIds();
        if (skills == null || skills.Count == 0)
        {
            lines.Add("(none)");
        }
        else
        {
            foreach (string skillId in skills)
                lines.Add(PlayerStatusLabels.FormatSkill(skillId, stats.GetSkillLevel(skillId)));
        }

        _skillsText.text = string.Join("\n", lines);
    }

    public void Wire(
        TMP_Text headerTitle,
        RectTransform bodyPartViewsRoot,
        TMP_Text vitalsText,
        TMP_Text skillsText,
        Button debugSeverArmLButton,
        TMP_Text debugSeverArmLLabel,
        UIPlayerStatusDetailPanel detailPanel,
        UIWindowDragHandler dragHandler)
    {
        _headerTitle = headerTitle;
        _bodyPartViewsRoot = bodyPartViewsRoot;
        _vitalsText = vitalsText;
        _skillsText = skillsText;
        _debugSeverArmLButton = debugSeverArmLButton;
        _debugSeverArmLLabel = debugSeverArmLLabel;
        _detailPanel = detailPanel;
        _windowDragHandler = dragHandler;
    }
}
