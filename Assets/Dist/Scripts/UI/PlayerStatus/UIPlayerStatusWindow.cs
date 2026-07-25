// ============================================================
// UIPlayerStatusWindow ??메인 6부??+ ?�역 바이??+ ?�킬 + ?�세 ?�널
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIPlayerStatusWindow : MonoBehaviour
{
    [SerializeField] TMP_Text _headerTitle;
    [SerializeField] RectTransform _bodyPartViewsRoot;
    [SerializeField] TMP_Text _vitalsText;
    [SerializeField] TMP_Text _skillsText;
    [SerializeField] Button _debugSeverArmLButton;
    [SerializeField] TMP_Text _debugSeverArmLLabel;
    [SerializeField] UIPlayerStatusDetailPanel _detailPanel;
    [SerializeField] UIWindowDragHandler _windowDragHandler;
    [SerializeField] UIWindowResizeHandler[] _resizeHandlers;

    readonly List<UIPlayerStatusBodyPartGraphic> _graphics = new(6);
    readonly List<UIPlayerStatusBodyPartRow> _rows = new(6);

    PlayerStatusViewModel _viewModel;

    public bool IsVisible => gameObject.activeSelf;
    public RectTransform WindowRect => transform as RectTransform;

    public void ConfigureChrome(Canvas rootCanvas)
    {
        if (_windowDragHandler == null)
            Debug.LogError("[UIPlayerStatusWindow] Window drag handler not assigned.", this);
        if (_resizeHandlers == null || _resizeHandlers.Length == 0)
            Debug.LogError("[UIPlayerStatusWindow] Resize handlers not assigned.", this);

        _windowDragHandler?.Initialize(WindowRect, rootCanvas);

        Vector2 minSize = new(PlayerStatusWindowLayout.MinWidth, PlayerStatusWindowLayout.MinHeight);
        Vector2 maxSize = PlayerStatusWindowLayout.GetMaxSize(rootCanvas);

        if (_resizeHandlers != null)
        {
            for (int i = 0; i < _resizeHandlers.Length; i++)
            {
                if (_resizeHandlers[i] == null)
                    continue;
                _resizeHandlers[i].Initialize(WindowRect, rootCanvas, minSize, maxSize);
            }
        }

        if (WindowRect != null && rootCanvas != null)
            WindowRect.sizeDelta = PlayerStatusWindowLayout.ClampSize(WindowRect.sizeDelta, rootCanvas);
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

        EnsurePartViews();
        Refresh();
    }

    public void Unbind()
    {
        if (_viewModel != null)
            _viewModel.Changed -= Refresh;
        if (_debugSeverArmLButton != null)
            _debugSeverArmLButton.onClick.RemoveListener(OnDebugSeverArmL);

        _viewModel = null;
        _detailPanel?.Hide();
    }

    void OnDestroy() => Unbind();

    void OnDebugSeverArmL()
    {
        _viewModel?.Body?.RemovePart(BodyPartIds.ArmL);
    }

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

                _graphics[i].Bind(partId, OnPartHover, OnPartExit);
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

    void OnPartHover(string partId)
    {
        ICharacterBody body = _viewModel?.Body;
        if (_detailPanel == null || body == null)
            return;

        _detailPanel.ShowForPart(body, partId);
    }

    void OnPartExit() => _detailPanel?.Hide();

    public void Refresh()
    {
        if (_viewModel == null)
            return;

        SetHeaderTitle(PlayerStatusLabels.Title);
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
            _graphics[i].SetDisplay(cur, max, present);
        }

        for (int i = 0; i < _rows.Count && i < mains.Length; i++)
        {
            string partId = mains[i];
            bool present = body != null && body.Has(partId);
            int cur = present ? body.GetConditionCur(partId) : 0;
            int max = present ? body.GetConditionMax(partId) : 0;
            _rows[i].SetDisplay(PlayerStatusLabels.GetPartName(partId), cur, max, present);
        }

        RefreshVitals();
        RefreshSkills();
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
        UIWindowDragHandler dragHandler,
        UIWindowResizeHandler[] resizeHandlers)
    {
        _headerTitle = headerTitle;
        _bodyPartViewsRoot = bodyPartViewsRoot;
        _vitalsText = vitalsText;
        _skillsText = skillsText;
        _debugSeverArmLButton = debugSeverArmLButton;
        _debugSeverArmLLabel = debugSeverArmLLabel;
        _detailPanel = detailPanel;
        _windowDragHandler = dragHandler;
        _resizeHandlers = resizeHandlers;
    }
}
