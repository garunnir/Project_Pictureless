// ============================================================
// InventoryWindowResizeHandler — 인벤 창 8방향 크기 조절
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;

public enum WindowResizeEdge
{
    Left,
    Right,
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public sealed class InventoryWindowResizeHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] WindowResizeEdge _edge = WindowResizeEdge.BottomRight;

    RectTransform _window;
    RectTransform _dragRoot;
    Canvas _canvas;
    Vector2 _startSize;
    Vector2 _startPosition;
    Vector2 _startPointerLocal;
    Vector2 _startPivot;
    Vector2 _minSize;
    Vector2 _maxSize;

    public WindowResizeEdge Edge => _edge;

    public void Initialize(RectTransform window, Canvas canvas, Vector2 minSize, Vector2 maxSize)
    {
        _window = window;
        _canvas = canvas;
        _dragRoot = window != null ? window.parent as RectTransform : null;
        _minSize = minSize;
        _maxSize = maxSize;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_window == null || _dragRoot == null)
            return;

        _startSize = _window.sizeDelta;
        _startPosition = _window.anchoredPosition;
        _startPivot = _window.pivot;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragRoot,
                eventData.position,
                GetEventCamera(eventData),
                out _startPointerLocal))
            _startPointerLocal = Vector2.zero;
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

        Vector2 delta = localPoint - _startPointerLocal;
        Vector2 rawSize = ComputeRawSize(_startSize, delta, _edge);
        Vector2 newSize = ClampSize(rawSize);
        Vector2 sizeChange = newSize - _startSize;

        _window.sizeDelta = newSize;
        _window.anchoredPosition = _startPosition + ComputePositionDelta(sizeChange, _edge, _startPivot);
    }

    static Vector2 ComputePositionDelta(Vector2 sizeChange, WindowResizeEdge edge, Vector2 pivot)
    {
        bool dragLeft = edge == WindowResizeEdge.Left ||
                        edge == WindowResizeEdge.TopLeft ||
                        edge == WindowResizeEdge.BottomLeft;
        bool dragRight = edge == WindowResizeEdge.Right ||
                         edge == WindowResizeEdge.TopRight ||
                         edge == WindowResizeEdge.BottomRight;
        bool dragTop = edge == WindowResizeEdge.Top ||
                       edge == WindowResizeEdge.TopLeft ||
                       edge == WindowResizeEdge.TopRight;
        bool dragBottom = edge == WindowResizeEdge.Bottom ||
                          edge == WindowResizeEdge.BottomLeft ||
                          edge == WindowResizeEdge.BottomRight;

        Vector2 delta = Vector2.zero;
        if (dragLeft && !dragRight)
            delta.x = -sizeChange.x * (1f - pivot.x);
        else if (dragRight && !dragLeft)
            delta.x = sizeChange.x * pivot.x;

        if (dragBottom && !dragTop)
            delta.y = -sizeChange.y * (1f - pivot.y);
        else if (dragTop && !dragBottom)
            delta.y = sizeChange.y * pivot.y;

        return delta;
    }

    static Vector2 ComputeRawSize(Vector2 startSize, Vector2 delta, WindowResizeEdge edge)
    {
        float width = startSize.x;
        float height = startSize.y;

        switch (edge)
        {
            case WindowResizeEdge.Left:
                width -= delta.x;
                break;
            case WindowResizeEdge.Right:
                width += delta.x;
                break;
            case WindowResizeEdge.Top:
                height += delta.y;
                break;
            case WindowResizeEdge.Bottom:
                height -= delta.y;
                break;
            case WindowResizeEdge.TopLeft:
                width -= delta.x;
                height += delta.y;
                break;
            case WindowResizeEdge.TopRight:
                width += delta.x;
                height += delta.y;
                break;
            case WindowResizeEdge.BottomLeft:
                width -= delta.x;
                height -= delta.y;
                break;
            case WindowResizeEdge.BottomRight:
                width += delta.x;
                height -= delta.y;
                break;
        }

        return new Vector2(width, height);
    }

    Vector2 ClampSize(Vector2 size)
    {
        size.x = Mathf.Clamp(size.x, _minSize.x, _maxSize.x);
        size.y = Mathf.Clamp(size.y, _minSize.y, _maxSize.y);
        return size;
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
