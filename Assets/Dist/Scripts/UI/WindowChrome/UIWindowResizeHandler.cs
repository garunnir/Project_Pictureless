// ============================================================
// UIWindowResizeHandler — 오버레이 창 8방향 크기 조절 (공용)
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

public sealed class UIWindowResizeHandler :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public const float DefaultRevealedAlpha = 0.35f;

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
    CanvasGroup _canvasGroup;
    Image _image;
    Color _baseImageColor;
    bool _hasBaseImageColor;
    float _revealedAlpha = DefaultRevealedAlpha;

    public WindowResizeEdge Edge => _edge;
    public bool IsDragging { get; private set; }

    public void SetEdge(WindowResizeEdge edge) => _edge = edge;

    public void Initialize(RectTransform window, Canvas canvas, Vector2 minSize, Vector2 maxSize)
    {
        _window = window;
        _canvas = canvas;
        _dragRoot = window != null ? window.parent as RectTransform : null;
        _minSize = minSize;
        _maxSize = maxSize;
        CacheVisuals();
    }

    /// <summary>
    /// 근접 리빌용. CanvasGroup이 있으면 alpha/blocksRaycasts, 없으면 Image alpha·raycast.
    /// </summary>
    public void SetVisualActive(bool active)
    {
        CacheVisuals();
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = active ? 1f : 0f;
            _canvasGroup.blocksRaycasts = active;
            _canvasGroup.interactable = active;
        }

        if (_image != null)
        {
            if (_canvasGroup == null)
            {
                Color c = _hasBaseImageColor ? _baseImageColor : _image.color;
                c.a = active ? Mathf.Max(_revealedAlpha, c.a) : 0f;
                _image.color = c;
            }

            _image.raycastTarget = active;
        }
    }

    public void SetRevealedAlpha(float alpha) =>
        _revealedAlpha = Mathf.Clamp01(alpha);

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_window == null || _dragRoot == null)
            return;

        IsDragging = true;
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

    public void OnEndDrag(PointerEventData eventData) => IsDragging = false;

    void OnDisable() => IsDragging = false;

    void CacheVisuals()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
        if (_image == null)
            _image = GetComponent<Image>();
        if (_image != null && !_hasBaseImageColor)
        {
            _baseImageColor = _image.color;
            _hasBaseImageColor = true;
        }
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
