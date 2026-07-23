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
    [SerializeField] UIWindowResizeHandler[] _resizeHandlers;

    TimeViewModel _viewModel;

    public RectTransform WindowRect => transform as RectTransform;

    public void Wire(
        TMP_Text timeText,
        UIWindowDragHandler dragHandler,
        UIWindowResizeProximity resizeProximity,
        UIWindowResizeHandler[] resizeHandlers)
    {
        _timeText = timeText;
        _dragHandler = dragHandler;
        _resizeProximity = resizeProximity;
        _resizeHandlers = resizeHandlers;
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
        if (_resizeProximity == null)
            Debug.LogError("[UITimeDisplayPanel] Resize proximity not assigned.", this);
        if (_resizeHandlers == null || _resizeHandlers.Length == 0)
            Debug.LogError("[UITimeDisplayPanel] Resize handlers not assigned.", this);

        _dragHandler?.Initialize(window, rootCanvas);

        if (_resizeHandlers != null)
        {
            for (int i = 0; i < _resizeHandlers.Length; i++)
            {
                if (_resizeHandlers[i] == null)
                    continue;
                _resizeHandlers[i].Initialize(
                    window,
                    rootCanvas,
                    TimeUIFactory.MinPanelSize,
                    TimeUIFactory.MaxPanelSize);
            }
        }

        if (_resizeProximity != null)
        {
            _resizeProximity.SetDragHeader(_dragHandler);
            _resizeProximity.SetResizeHandlers(_resizeHandlers);
            _resizeProximity.Initialize(
                window,
                rootCanvas,
                TimeUIFactory.ResizeProximityPadding);
            _resizeProximity.SetProximityEnabled(enableDragHeader || enableResize);
            _resizeProximity.SetHeaderProximityActive(enableDragHeader);
            _resizeProximity.SetResizeHandlesActive(enableResize);
        }
        else if (_resizeHandlers != null && !enableResize)
        {
            for (int i = 0; i < _resizeHandlers.Length; i++)
                _resizeHandlers[i]?.SetVisualActive(false);
        }
    }

    public void Refresh()
    {
        if (_timeText == null)
            return;

        _timeText.text = _viewModel != null
            ? _viewModel.DisplayText
            : TimeDisplayFormat.Format(0, 0, 0);
    }
}
