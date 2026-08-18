// ============================================================

// UISettingsWindow — 세팅 Overlay 카테고리·Graphics/HUD 조정·HUD 팝업 토글

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

    [SerializeField] Toggle _hudLayoutToggle;

    [SerializeField] Toggle _hudTimeToggle;

    [SerializeField] Toggle _hudTimeScaleToggle;

    [SerializeField] Toggle _hudMessageLogToggle;

    [SerializeField] Toggle _hudSummaryToggle;



    Action _onClose;

    bool _bound;



    public RectTransform WindowRect => _window != null ? _window : transform as RectTransform;



    public void Wire(

        RectTransform window,

        UIWindowDragHandler dragHandler,

        TMP_Text title,

        Button graphicsButton,

        GameObject graphicsPage,

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

        ShowGraphicsPage(true);

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



        if (_graphicsButton != null)

        {

            TMP_Text label = _graphicsButton.GetComponentInChildren<TMP_Text>(true);

            if (label != null)

            {

                DistUiFont.Apply(label);

                label.text = SettingsLabels.CategoryGraphics;

            }

        }



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



    public void ShowGraphicsPage(bool show)

    {

        if (_graphicsPage != null)

            _graphicsPage.SetActive(show);

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



        if (_graphicsButton != null)

        {

            if (bind)

                _graphicsButton.onClick.AddListener(OnGraphicsClicked);

            else

                _graphicsButton.onClick.RemoveListener(OnGraphicsClicked);

        }



        BindToggle(_hudLayoutToggle, bind, OnHudLayoutToggleChanged);

        BindToggle(_hudTimeToggle, bind, OnHudTimeToggleChanged);

        BindToggle(_hudTimeScaleToggle, bind, OnHudTimeScaleToggleChanged);

        BindToggle(_hudMessageLogToggle, bind, OnHudMessageLogToggleChanged);

        BindToggle(_hudSummaryToggle, bind, OnHudSummaryToggleChanged);



        _bound = bind;

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



    void OnGraphicsClicked() => ShowGraphicsPage(true);



    void OnHudLayoutToggleChanged(bool on)
    {
        HudLayoutEdit.SetActive(on);
        // 같은 값으로 클릭되는(또는 정적 상태가 어긋나는) 케이스에서도
        // 참가자들의 히트/가시 상태를 동기화한다.
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

