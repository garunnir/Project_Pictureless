// ============================================================
// UITimeScaleHudPanel — 배속 HUD 4버튼 + 선택 강조
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UITimeScaleHudPanel : MonoBehaviour
{
    [SerializeField] Button _pauseButton;
    [SerializeField] Button _normalButton;
    [SerializeField] Button _doubleButton;
    [SerializeField] Button _smartButton;
    [SerializeField] UIWindowDragHandler _headerDrag;
    [SerializeField] UIWindowDragHandler _layoutDrag;
    [SerializeField] UIWindowResizeHandles _resizeHandles;
    [SerializeField] UIWindowResizeProximity _resizeProximity;

    GameplayTimeScale _timeScale;
    bool _bound;

    public void Wire(
        Button pauseButton,
        Button normalButton,
        Button doubleButton,
        Button smartButton,
        UIWindowDragHandler headerDrag,
        UIWindowDragHandler layoutDrag,
        UIWindowResizeHandles resizeHandles,
        UIWindowResizeProximity resizeProximity)
    {
        _pauseButton = pauseButton;
        _normalButton = normalButton;
        _doubleButton = doubleButton;
        _smartButton = smartButton;
        _headerDrag = headerDrag;
        _layoutDrag = layoutDrag;
        _resizeHandles = resizeHandles;
        _resizeProximity = resizeProximity;
    }

    public void BindGameplayTimeScale(GameplayTimeScale timeScale)
    {
        if (_timeScale == timeScale)
            return;

        if (_timeScale != null)
            _timeScale.Changed -= RefreshSelection;

        _timeScale = timeScale;
        if (_timeScale != null)
            _timeScale.Changed += RefreshSelection;
    }

    public void ConfigureWindowChrome(Canvas rootCanvas)
    {
        RectTransform window = transform as RectTransform;
        _headerDrag?.Initialize(window, rootCanvas);
        _layoutDrag?.Initialize(window, rootCanvas);
        _resizeHandles?.Initialize(
            window,
            rootCanvas,
            TimeScaleHudLayout.PanelSize,
            TimeUIFactory.MaxPanelSize);
        _resizeProximity?.Initialize(
            window,
            rootCanvas,
            TimeUIFactory.ResizeProximityPadding);
        _resizeProximity?.SetDragHeader(_headerDrag);
        if (_resizeHandles != null)
            _resizeProximity?.SetResizeHandlers(_resizeHandles.Handlers);
    }

    void Awake()
    {
        RefreshLabels();
        BindButtons(true);
        RefreshSelection();
    }

    void OnDestroy()
    {
        BindButtons(false);
        if (_timeScale != null)
            _timeScale.Changed -= RefreshSelection;
    }

    public void RefreshLabels()
    {
        SetButtonLabel(_pauseButton, TimeScaleHudLabels.Pause);
        SetButtonLabel(_normalButton, TimeScaleHudLabels.Normal);
        SetButtonLabel(_doubleButton, TimeScaleHudLabels.Double);
        SetButtonLabel(_smartButton, TimeScaleHudLabels.Smart);
    }

    void BindButtons(bool bind)
    {
        if (bind == _bound)
            return;

        BindOne(_pauseButton, bind, OnPauseClicked);
        BindOne(_normalButton, bind, OnNormalClicked);
        BindOne(_doubleButton, bind, OnDoubleClicked);
        BindOne(_smartButton, bind, OnSmartClicked);
        _bound = bind;
    }

    static void BindOne(Button button, bool bind, UnityEngine.Events.UnityAction handler)
    {
        if (button == null)
            return;

        if (bind)
            button.onClick.AddListener(handler);
        else
            button.onClick.RemoveListener(handler);
    }

    void OnPauseClicked() => _timeScale?.SetMode(GameplayTimeScale.Mode.Pause);
    void OnNormalClicked() => _timeScale?.SetMode(GameplayTimeScale.Mode.Normal);
    void OnDoubleClicked() => _timeScale?.SetMode(GameplayTimeScale.Mode.Double);
    void OnSmartClicked() => _timeScale?.SetMode(GameplayTimeScale.Mode.Smart);

    void RefreshSelection()
    {
        GameplayTimeScale.Mode mode = _timeScale != null
            ? _timeScale.CurrentMode
            : GameplayTimeScale.Mode.Normal;

        ApplySelected(_pauseButton, mode == GameplayTimeScale.Mode.Pause);
        ApplySelected(_normalButton, mode == GameplayTimeScale.Mode.Normal);
        ApplySelected(_doubleButton, mode == GameplayTimeScale.Mode.Double);
        ApplySelected(_smartButton, mode == GameplayTimeScale.Mode.Smart);
    }

    static void SetButtonLabel(Button button, string text)
    {
        if (button == null)
            return;

        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp == null)
            return;

        DistUiFont.Apply(tmp);
        tmp.text = text;
    }

    static void ApplySelected(Button button, bool selected)
    {
        if (button == null)
            return;

        Image image = button.targetGraphic as Image;
        if (image != null)
            image.color = selected ? TimeScaleHudLayout.SelectedColor : TimeScaleHudLayout.NormalColor;
    }
}
