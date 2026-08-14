// ============================================================
// UITimeDisplayPanel — 시계 HUD 텍스트 패널 + 창 크롬
// ============================================================

using TMPro;
using UnityEngine;

public sealed class UITimeDisplayPanel : MonoBehaviour
{
    [SerializeField] TMP_Text _timeText;

    [Header("Window Chrome")]
    [SerializeField] UIWindowDragHandler _dragHandler;
    [SerializeField] UIWindowResizeProximity _resizeProximity;
    [SerializeField] UIWindowResizeHandles _resizeHandles;
    UIWindowChromeBar _chromeBar;

    TimeViewModel _viewModel;

    public RectTransform WindowRect => transform as RectTransform;

    public void Wire(
        TMP_Text timeText,
        UIWindowDragHandler dragHandler,
        UIWindowResizeProximity resizeProximity,
        UIWindowResizeHandles resizeHandles)
    {
        _timeText = timeText;
        _dragHandler = dragHandler;
        _resizeProximity = resizeProximity;
        _resizeHandles = resizeHandles;
    }

    public void BindViewModel(TimeViewModel viewModel) => _viewModel = viewModel;

    public void ConfigureWindowChrome(
        Canvas rootCanvas,
        bool enableDragHeader,
        bool enableResize)
    {
        RectTransform window = WindowRect;

        if (_dragHandler == null)
            Debug.LogError("[UITimeDisplayPanel] Drag handler not assigned.", this);
        if (_resizeHandles == null)
            _resizeHandles = GetComponent<UIWindowResizeHandles>();
        if (_resizeProximity == null)
            _resizeProximity = GetComponent<UIWindowResizeProximity>();

        if (_resizeHandles == null)
            Debug.LogError("[UITimeDisplayPanel] UIWindowResizeHandles not assigned.", this);
        if (_resizeProximity == null)
            Debug.LogError("[UITimeDisplayPanel] Resize proximity not assigned.", this);

        _dragHandler?.Initialize(window, rootCanvas);
        _dragHandler?.SetProximityPadding(TimeUIFactory.ResizeProximityPadding);
        _dragHandler?.SetProximityRevealEnabled(enableDragHeader);

        _resizeHandles?.Initialize(
            window,
            rootCanvas,
            TimeUIFactory.MinPanelSize,
            TimeUIFactory.MaxPanelSize);
        _resizeHandles?.SetProximityReveal(enableResize);
        _resizeHandles?.SetHandlesActive(enableResize);

        if (_resizeProximity != null)
        {
            _resizeProximity.SetDragHeader(_dragHandler);
            if (_resizeHandles != null)
                _resizeProximity.SetResizeHandlers(_resizeHandles.Handlers);
            _resizeProximity.Initialize(
                window,
                rootCanvas,
                TimeUIFactory.ResizeProximityPadding);
            _resizeProximity.SetProximityEnabled(enableResize);
            _resizeProximity.SetResizeHandlesActive(enableResize);
        }

        if (!TryGetComponent(out UIOverlayWindow _))
            Debug.LogError("[UITimeDisplayPanel] UIOverlayWindow missing on HUD prefab root.", this);

        _chromeBar = GetComponentInChildren<UIWindowChromeBar>(true);
        UIWindowChromeBar.BindCloseOnWindow(this, Hide);
    }

    public void Hide() => gameObject.SetActive(false);

    public void Refresh()
    {
        if (_timeText == null)
            return;

        _timeText.text = _viewModel != null
            ? _viewModel.DisplayText
            : TimeDisplayFormat.Format(0, 0, 0);
        _chromeBar?.SetFoldedTitle(_timeText.text);
    }
}
