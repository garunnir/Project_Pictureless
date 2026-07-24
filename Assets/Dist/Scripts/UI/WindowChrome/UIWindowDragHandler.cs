// ============================================================
// UIWindowDragHandler — 오버레이 창 헤더 드래그 이동 (공용)
// ============================================================

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[InfoBox(
    "헤더에 부착. Window를 지정하면 드래그가 단독으로 동작합니다. " +
    "Proximity Reveal을 켜면 헤더 나타남/사라짐도 이 컴포넌트만으로 동작합니다. " +
    "Canvas는 비우면 부모에서 찾고, Initialize로 런타임 주입도 가능합니다. " +
    "같은 GO에 raycast용 Image(또는 Graphic)가 필요합니다.",
    InfoMessageType.Info)]
public sealed class UIWindowDragHandler :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public const float DefaultRevealedAlpha = 0.9f;
    public const float DefaultProximityPadding = 12f;

    [SerializeField, Required] RectTransform _window;
    [SerializeField] Canvas _canvas;

    [Header("Proximity Reveal (opt-in)")]
    [Tooltip("true = 창/상단 근접 시에만 헤더 가시·히트. false = 상시(호출측 SetVisualActive).")]
    [SerializeField] bool _proximityReveal;

    [SerializeField] float _proximityPadding = DefaultProximityPadding;

    RectTransform _dragRoot;
    Vector2 _dragOffset;
    CanvasGroup _canvasGroup;
    Image _image;
    Color _baseImageColor;
    bool _hasBaseImageColor;
    float _revealedAlpha = DefaultRevealedAlpha;
    bool _proximitySuppressed;

    public bool IsDragging { get; private set; }
    public bool IsProximityRevealEnabled => _proximityReveal;

    void Awake()
    {
        EnsureReady(resolveCanvasIfMissing: true);
        if (_proximityReveal)
            SetVisualActive(false);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        RefreshDragRoot();
        CacheVisuals();
        _proximityPadding = Mathf.Max(0f, _proximityPadding);
    }
#endif

    /// <summary>SerializeField 대입 또는 런타임 Canvas 주입용. null은 해당 필드를 유지합니다.</summary>
    public void Initialize(RectTransform window, Canvas canvas)
    {
        if (window != null)
            _window = window;
        if (canvas != null)
            _canvas = canvas;
        EnsureReady(resolveCanvasIfMissing: true);
    }

    public void SetProximityRevealEnabled(bool enabled)
    {
        _proximityReveal = enabled;
        if (!enabled)
            return;

        if (!IsDragging && !_proximitySuppressed)
            SetVisualActive(false);
    }

    public void SetProximityPadding(float padding) =>
        _proximityPadding = Mathf.Max(0f, padding);

    /// <summary>리사이즈 등 외부 조율용. true면 근접 리빌을 숨김으로 고정.</summary>
    public void SetProximitySuppressed(bool suppressed)
    {
        _proximitySuppressed = suppressed;
        if (suppressed)
            SetVisualActive(false);
    }

    /// <summary>가시·히트. CanvasGroup 우선, 없으면 Image alpha·raycast.</summary>
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
        EnsureReady(resolveCanvasIfMissing: true);
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

    void OnDisable()
    {
        IsDragging = false;
        _proximitySuppressed = false;
    }

    void LateUpdate()
    {
        if (!_proximityReveal || _window == null)
            return;

        if (_proximitySuppressed)
        {
            SetVisualActive(false);
            return;
        }

        if (IsDragging)
        {
            SetVisualActive(true);
            return;
        }

        if (!TryGetPointerLocal(out Vector2 local))
        {
            SetVisualActive(false);
            return;
        }

        SetVisualActive(IsNearOrInsideWindow(local) || IsNearTop(local));
    }

    void EnsureReady(bool resolveCanvasIfMissing)
    {
        RefreshDragRoot();
        if (resolveCanvasIfMissing && _canvas == null)
            _canvas = GetComponentInParent<Canvas>();
        CacheVisuals();
    }

    void RefreshDragRoot() =>
        _dragRoot = _window != null ? _window.parent as RectTransform : null;

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

    bool TryGetPointerLocal(out Vector2 local)
    {
        local = default;
        EnsureReady(resolveCanvasIfMissing: true);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _window,
            ResolvePointerScreen(),
            ResolveCamera(),
            out local);
    }

    bool IsNearOrInsideWindow(Vector2 local)
    {
        Rect r = _window.rect;
        float pad = _proximityPadding;
        return local.x >= r.xMin - pad &&
               local.x <= r.xMax + pad &&
               local.y >= r.yMin - pad &&
               local.y <= r.yMax + pad;
    }

    bool IsNearTop(Vector2 local)
    {
        if (!IsNearOrInsideWindow(local))
            return false;
        Rect r = _window.rect;
        return Mathf.Abs(local.y - r.yMax) <= _proximityPadding;
    }

    Vector2 ResolvePointerScreen()
    {
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
        return Input.mousePosition;
    }

    Camera ResolveCamera()
    {
        if (_canvas == null)
            return null;
        return _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;
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
