// ============================================================
// UIWindowDragHandler — 오버레이 창 헤더 드래그 이동 (공용)
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UIWindowDragHandler :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public const float DefaultRevealedAlpha = 0.9f;

    RectTransform _window;
    RectTransform _dragRoot;
    Canvas _canvas;
    Vector2 _dragOffset;
    CanvasGroup _canvasGroup;
    Image _image;
    Color _baseImageColor;
    bool _hasBaseImageColor;
    float _revealedAlpha = DefaultRevealedAlpha;

    public bool IsDragging { get; private set; }

    public void Initialize(RectTransform window, Canvas canvas)
    {
        _window = window;
        _canvas = canvas;
        _dragRoot = window != null ? window.parent as RectTransform : null;
        CacheVisuals();
    }

    /// <summary>근접 리빌용. CanvasGroup 우선, 없으면 Image alpha·raycast.</summary>
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

    Camera GetEventCamera(PointerEventData eventData)
    {
        if (_canvas == null)
            return eventData.pressEventCamera;

        return _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera != null ? _canvas.worldCamera : eventData.pressEventCamera;
    }
}
