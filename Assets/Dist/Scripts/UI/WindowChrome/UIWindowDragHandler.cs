// ============================================================
// UIWindowDragHandler — 오버레이 창 헤더 드래그 이동 (공용)
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UIWindowDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    RectTransform _window;
    RectTransform _dragRoot;
    Canvas _canvas;
    Vector2 _dragOffset;

    public void Initialize(RectTransform window, Canvas canvas)
    {
        _window = window;
        _canvas = canvas;
        _dragRoot = window != null ? window.parent as RectTransform : null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_window == null || _dragRoot == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragRoot,
                eventData.position,
                GetEventCamera(eventData),
                out Vector2 localPoint))
            return;

        _dragOffset = _window.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_window == null || _dragRoot == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragRoot,
                eventData.position,
                GetEventCamera(eventData),
                out Vector2 localPoint))
            return;

        _window.anchoredPosition = localPoint + _dragOffset;
    }

    Camera GetEventCamera(PointerEventData eventData)
    {
        if (_canvas == null)
            return eventData.pressEventCamera;

        return _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera != null ? _canvas.worldCamera : eventData.pressEventCamera;
    }
}
