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

    TimeViewModel _viewModel;

    public RectTransform WindowRect => transform as RectTransform;

    public void Wire(
        TMP_Text timeText,
        UIWindowDragHandler dragHandler,
        UIWindowResizeProximity resizeProximity)
    {
        _timeText = timeText;
        _dragHandler = dragHandler;
        _resizeProximity = resizeProximity;
    }

    public void BindViewModel(TimeViewModel viewModel) => _viewModel = viewModel;

    public void ConfigureWindowChrome(
        Canvas rootCanvas,
        bool enableDragHeader,
        bool enableResize)
    {
        RectTransform window = WindowRect;
        if (_dragHandler == null)
            _dragHandler = GetComponentInChildren<UIWindowDragHandler>(true);
        if (_resizeProximity == null)
            _resizeProximity = GetComponent<UIWindowResizeProximity>();

        _dragHandler?.Initialize(window, rootCanvas);

        UIWindowResizeHandler[] resizeHandlers =
            GetComponentsInChildren<UIWindowResizeHandler>(true);
        for (int i = 0; i < resizeHandlers.Length; i++)
            resizeHandlers[i].Initialize(
                window,
                rootCanvas,
                TimeUIFactory.MinPanelSize,
                TimeUIFactory.MaxPanelSize);

        if (_resizeProximity != null)
        {
            _resizeProximity.SetDragHeader(_dragHandler);
            _resizeProximity.Initialize(
                window,
                rootCanvas,
                TimeUIFactory.ResizeProximityPadding);
            _resizeProximity.SetProximityEnabled(enableDragHeader || enableResize);
            _resizeProximity.SetHeaderProximityActive(enableDragHeader);
            _resizeProximity.SetResizeHandlesActive(enableResize);
        }
        else
        {
            if (!enableDragHeader)
                _dragHandler?.SetVisualActive(false);
            if (!enableResize)
            {
                for (int i = 0; i < resizeHandlers.Length; i++)
                    resizeHandlers[i].SetVisualActive(false);
            }
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
