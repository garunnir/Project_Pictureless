// ============================================================
// UISettingsWindow — 세팅 Overlay 카테고리·Graphics/Game·HUD 조정
// ============================================================

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UISettingsWindow : MonoBehaviour
{
    [SerializeField] RectTransform _window;
    [SerializeField] UIWindowDragHandler _dragHandler;
    [SerializeField] TMP_Text _title;
    [SerializeField] Button _graphicsButton;
    [SerializeField] GameObject _graphicsPage;
    [SerializeField] Button _gameButton;
    [SerializeField] GameObject _gamePage;
    [SerializeField] Button _saveButton;
    [SerializeField] Button _loadButton;
    [SerializeField] Toggle _hudLayoutToggle;
    [SerializeField] Toggle _hudTimeToggle;
    [SerializeField] Toggle _hudTimeScaleToggle;
    [SerializeField] Toggle _hudMessageLogToggle;
    [SerializeField] Toggle _hudSummaryToggle;

    Action _onClose;
    bool _bound;

    public event Action SaveClicked;
    public event Action LoadClicked;

    public RectTransform WindowRect => _window != null ? _window : transform as RectTransform;

    public void Wire(
        RectTransform window,
        UIWindowDragHandler dragHandler,
        TMP_Text title,
        Button graphicsButton,
        GameObject graphicsPage,
        Button gameButton,
        GameObject gamePage,
        Button saveButton,
        Button loadButton,
        Toggle hudLayoutToggle,
        Toggle hudTimeToggle,
        Toggle hudTimeScaleToggle,
        Toggle hudMessageLogToggle,
        Toggle hudSummaryToggle)
    {
        _window = window;
        _dragHandler = dragHandler;
        _title = title;
        _graphicsButton = graphicsButton;
        _graphicsPage = graphicsPage;
        _gameButton = gameButton;
        _gamePage = gamePage;
        _saveButton = saveButton;
        _loadButton = loadButton;
        _hudLayoutToggle = hudLayoutToggle;
        _hudTimeToggle = hudTimeToggle;
        _hudTimeScaleToggle = hudTimeScaleToggle;
        _hudMessageLogToggle = hudMessageLogToggle;
        _hudSummaryToggle = hudSummaryToggle;
    }

    void Awake()
    {
        BindControls(true);
        RefreshLabels();
        SyncHudPopupToggles(notify: false);
        ShowGraphicsPage();
    }

    void OnDestroy() => BindControls(false);

    public void BindClose(Action onClose) => _onClose = onClose;

    public void ConfigureChrome(Canvas rootCanvas)
    {
        _dragHandler?.Initialize(WindowRect, rootCanvas);
        UIWindowChromeBar.BindCloseOnWindow(this, () => _onClose?.Invoke());
    }

    public void RefreshLabels()
    {
        if (_title != null)
        {
            DistUiFont.Apply(_title);
            _title.text = SettingsLabels.WindowTitle;
        }

        SetButtonLabel(_graphicsButton, SettingsLabels.CategoryGraphics);
        SetButtonLabel(_gameButton, GameSaveLabels.CategoryGame);
        SetButtonLabel(_saveButton, GameSaveLabels.Save);
        SetButtonLabel(_loadButton, GameSaveLabels.Load);

        SetToggleLabel(_hudLayoutToggle, SettingsLabels.HudLayoutAdjust);
        SetToggleLabel(_hudTimeToggle, SettingsLabels.HudTime);
        SetToggleLabel(_hudTimeScaleToggle, SettingsLabels.HudTimeScale);
        SetToggleLabel(_hudMessageLogToggle, SettingsLabels.HudMessageLog);
        SetToggleLabel(_hudSummaryToggle, SettingsLabels.HudSummary);
    }

    public void SyncHudPopupToggles(bool notify)
    {
        SyncOneToggle(_hudTimeToggle, HudLayoutIds.TimeDisplay, notify);
        SyncOneToggle(_hudTimeScaleToggle, HudLayoutIds.TimeScaleHud, notify);
        SyncOneToggle(_hudMessageLogToggle, HudLayoutIds.MessageLog, notify);
        SyncOneToggle(_hudSummaryToggle, HudLayoutIds.PlayerStatusSummary, notify);
    }

    public void ShowGraphicsPage()
    {
        if (_graphicsPage != null)
            _graphicsPage.SetActive(true);
        if (_gamePage != null)
            _gamePage.SetActive(false);
    }

    public void ShowGamePage()
    {
        if (_graphicsPage != null)
            _graphicsPage.SetActive(false);
        if (_gamePage != null)
            _gamePage.SetActive(true);
    }

    public void SetHudLayoutToggle(bool on, bool notify)
    {
        if (_hudLayoutToggle == null)
            return;

        if (notify)
            _hudLayoutToggle.isOn = on;
        else
            _hudLayoutToggle.SetIsOnWithoutNotify(on);
    }

    public bool IsHudLayoutToggleOn => _hudLayoutToggle != null && _hudLayoutToggle.isOn;

    void BindControls(bool bind)
    {
        if (bind == _bound)
            return;

        BindButton(_graphicsButton, bind, OnGraphicsClicked);
        BindButton(_gameButton, bind, OnGameClicked);
        BindButton(_saveButton, bind, OnSaveClicked);
        BindButton(_loadButton, bind, OnLoadClicked);

        BindToggle(_hudLayoutToggle, bind, OnHudLayoutToggleChanged);
        BindToggle(_hudTimeToggle, bind, OnHudTimeToggleChanged);
        BindToggle(_hudTimeScaleToggle, bind, OnHudTimeScaleToggleChanged);
        BindToggle(_hudMessageLogToggle, bind, OnHudMessageLogToggleChanged);
        BindToggle(_hudSummaryToggle, bind, OnHudSummaryToggleChanged);

        _bound = bind;
    }

    static void BindButton(Button button, bool bind, UnityEngine.Events.UnityAction handler)
    {
        if (button == null)
            return;

        if (bind)
            button.onClick.AddListener(handler);
        else
            button.onClick.RemoveListener(handler);
    }

    static void BindToggle(Toggle toggle, bool bind, UnityEngine.Events.UnityAction<bool> handler)
    {
        if (toggle == null)
            return;

        if (bind)
            toggle.onValueChanged.AddListener(handler);
        else
            toggle.onValueChanged.RemoveListener(handler);
    }

    static void SetButtonLabel(Button button, string text)
    {
        if (button == null)
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            return;

        DistUiFont.Apply(label);
        label.text = text;
    }

    static void SetToggleLabel(Toggle toggle, string text)
    {
        if (toggle == null)
            return;

        TMP_Text label = toggle.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            return;

        DistUiFont.Apply(label);
        label.text = text;
    }

    static void SyncOneToggle(Toggle toggle, string participantId, bool notify)
    {
        if (toggle == null)
            return;

        bool visible = HudPopupVisibility.IsVisible(participantId);
        if (notify)
            toggle.isOn = visible;
        else
            toggle.SetIsOnWithoutNotify(visible);
    }

    void OnGraphicsClicked() => ShowGraphicsPage();

    void OnGameClicked() => ShowGamePage();

    void OnSaveClicked() => SaveClicked?.Invoke();

    void OnLoadClicked() => LoadClicked?.Invoke();

    void OnHudLayoutToggleChanged(bool on)
    {
        HudLayoutEdit.SetActive(on);
        HudLayoutEdit.Refresh();
    }

    void OnHudTimeToggleChanged(bool on) =>
        HudPopupVisibility.SetVisible(HudLayoutIds.TimeDisplay, on);

    void OnHudTimeScaleToggleChanged(bool on) =>
        HudPopupVisibility.SetVisible(HudLayoutIds.TimeScaleHud, on);

    void OnHudMessageLogToggleChanged(bool on) =>
        HudPopupVisibility.SetVisible(HudLayoutIds.MessageLog, on);

    void OnHudSummaryToggleChanged(bool on) =>
        HudPopupVisibility.SetVisible(HudLayoutIds.PlayerStatusSummary, on);
}
