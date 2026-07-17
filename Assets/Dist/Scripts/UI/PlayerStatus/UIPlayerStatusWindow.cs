// ============================================================
// UIPlayerStatusWindow — 메인 6부위 + 전역 바이탈 + 스킬 + 상세 패널
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIPlayerStatusWindow : MonoBehaviour
{
    [SerializeField] TMP_Text _headerTitle;
    [SerializeField] RectTransform _bodyPartRowsRoot;
    [SerializeField] TMP_Text _vitalsText;
    [SerializeField] TMP_Text _skillsText;
    [SerializeField] Button _debugSeverArmLButton;
    [SerializeField] TMP_Text _debugSeverArmLLabel;
    [SerializeField] UIPlayerStatusDetailPanel _detailPanel;
    [SerializeField] UIWindowDragHandler _windowDragHandler;

    readonly List<UIPlayerStatusBodyPartRow> _rows = new(6);

    IPlayerBody _body;
    IPlayerVitals _vitals;
    IPlayerStats _stats;

    public bool IsVisible => gameObject.activeSelf;
    public RectTransform WindowRect => transform as RectTransform;

    public void ConfigureChrome(Canvas rootCanvas)
    {
        if (_windowDragHandler == null)
            _windowDragHandler = GetComponentInChildren<UIWindowDragHandler>(true);
        _windowDragHandler?.Initialize(WindowRect, rootCanvas);

        Vector2 minSize = new(PlayerStatusWindowLayout.MinWidth, PlayerStatusWindowLayout.MinHeight);
        Vector2 maxSize = PlayerStatusWindowLayout.GetMaxSize(rootCanvas);

        UIWindowResizeHandler[] resizeHandlers =
            GetComponentsInChildren<UIWindowResizeHandler>(true);
        for (int i = 0; i < resizeHandlers.Length; i++)
            resizeHandlers[i].Initialize(WindowRect, rootCanvas, minSize, maxSize);

        if (WindowRect != null && rootCanvas != null)
            WindowRect.sizeDelta = PlayerStatusWindowLayout.ClampSize(WindowRect.sizeDelta, rootCanvas);
    }

    public void SetHeaderTitle(string title)
    {
        if (_headerTitle != null)
            _headerTitle.text = title;
    }

    public void Initialize(IPlayerBody body, IPlayerVitals vitals, IPlayerStats stats)
    {
        Unbind();
        _body = body;
        _vitals = vitals;
        _stats = stats;

        if (_body != null)
            _body.Changed += Refresh;
        if (_vitals != null)
            _vitals.Changed += OnVitalChanged;

        if (_debugSeverArmLButton != null)
        {
            _debugSeverArmLButton.onClick.RemoveListener(OnDebugSeverArmL);
            _debugSeverArmLButton.onClick.AddListener(OnDebugSeverArmL);
        }

        if (_debugSeverArmLLabel != null)
            _debugSeverArmLLabel.text = PlayerStatusLabels.DebugSeverArmL;

        EnsureRows();
        Refresh();
    }

    public void Unbind()
    {
        if (_body != null)
            _body.Changed -= Refresh;
        if (_vitals != null)
            _vitals.Changed -= OnVitalChanged;
        if (_debugSeverArmLButton != null)
            _debugSeverArmLButton.onClick.RemoveListener(OnDebugSeverArmL);

        _body = null;
        _vitals = null;
        _stats = null;
        _detailPanel?.Hide();
    }

    void OnDestroy() => Unbind();

    void OnVitalChanged(string _) => Refresh();

    void OnDebugSeverArmL()
    {
        _body?.RemovePart(BodyPartIds.ArmL);
    }

    void EnsureRows()
    {
        if (_bodyPartRowsRoot == null)
            return;

        if (_rows.Count > 0)
            return;

        UIPlayerStatusBodyPartRow[] existing =
            _bodyPartRowsRoot.GetComponentsInChildren<UIPlayerStatusBodyPartRow>(true);
        for (int i = 0; i < existing.Length; i++)
            _rows.Add(existing[i]);

        string[] mains = BodyPartIds.MainHpParts;
        while (_rows.Count < mains.Length)
        {
            UIPlayerStatusBodyPartRow row = PlayerStatusUIFactory.CreateBodyPartRow(_bodyPartRowsRoot);
            _rows.Add(row);
        }

        for (int i = 0; i < mains.Length && i < _rows.Count; i++)
            _rows[i].Bind(mains[i], OnRowHover, OnRowExit);
    }

    void OnRowHover(string partId)
    {
        if (_detailPanel == null || _body == null)
            return;

        Vector2 tipPos = new(220f, 40f);
        _detailPanel.ShowForPart(_body, partId, tipPos);
    }

    void OnRowExit() => _detailPanel?.Hide();

    public void Refresh()
    {
        SetHeaderTitle(PlayerStatusLabels.Title);
        EnsureRows();

        string[] mains = BodyPartIds.MainHpParts;
        for (int i = 0; i < _rows.Count && i < mains.Length; i++)
        {
            string partId = mains[i];
            bool present = _body != null && _body.Has(partId);
            int cur = present ? _body.GetHpCur(partId) : 0;
            int max = present ? _body.GetHpMax(partId) : 0;
            _rows[i].SetDisplay(PlayerStatusLabels.GetPartName(partId), cur, max, present);
        }

        RefreshVitals();
        RefreshSkills();
    }

    void RefreshVitals()
    {
        if (_vitalsText == null)
            return;

        if (_vitals == null)
        {
            _vitalsText.text = string.Empty;
            return;
        }

        var lines = new List<string>(VitalKeys.All.Length + 1)
        {
            PlayerStatusLabels.VitalsSection
        };

        for (int i = 0; i < VitalKeys.All.Length; i++)
        {
            string key = VitalKeys.All[i];
            lines.Add(
                $"{PlayerStatusLabels.GetVitalName(key)}  " +
                PlayerStatusLabels.FormatVital(_vitals.GetCurrent(key), _vitals.GetMax(key)));
        }

        _vitalsText.text = string.Join("\n", lines);
    }

    void RefreshSkills()
    {
        if (_skillsText == null)
            return;

        if (_stats == null)
        {
            _skillsText.text = string.Empty;
            return;
        }

        var lines = new List<string> { PlayerStatusLabels.SkillsSection };
        IReadOnlyCollection<string> skills = _stats.GetKnownSkillIds();
        if (skills == null || skills.Count == 0)
        {
            lines.Add("—");
        }
        else
        {
            foreach (string skillId in skills)
                lines.Add(PlayerStatusLabels.FormatSkill(skillId, _stats.GetSkillLevel(skillId)));
        }

        _skillsText.text = string.Join("\n", lines);
    }

    public void Wire(
        TMP_Text headerTitle,
        RectTransform bodyPartRowsRoot,
        TMP_Text vitalsText,
        TMP_Text skillsText,
        Button debugSeverArmLButton,
        TMP_Text debugSeverArmLLabel,
        UIPlayerStatusDetailPanel detailPanel,
        UIWindowDragHandler dragHandler)
    {
        _headerTitle = headerTitle;
        _bodyPartRowsRoot = bodyPartRowsRoot;
        _vitalsText = vitalsText;
        _skillsText = skillsText;
        _debugSeverArmLButton = debugSeverArmLButton;
        _debugSeverArmLLabel = debugSeverArmLLabel;
        _detailPanel = detailPanel;
        _windowDragHandler = dragHandler;
    }
}
