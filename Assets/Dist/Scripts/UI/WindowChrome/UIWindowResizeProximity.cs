// ============================================================
// UIWindowResizeProximity — 창 가장자리 근접 시 리사이즈 핸들 표시 (공용, 옵트인)
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 창 루트에 1개. 기본 비활성(옵트인).
/// 리사이즈 핸들 근접 리빌만 담당. 헤더 나타남/사라짐은 UIWindowDragHandler 자체 옵션.
/// 선택적으로 DragHeader를 받아: 헤더 드래그 중 핸들 숨김, 리사이즈 중 헤더 억제.
/// Handlers/Window/Canvas는 비우면 같은 GO UIWindowResizeHandles·자신·부모에서 해석.
/// </summary>
[DisallowMultipleComponent]
public sealed class UIWindowResizeProximity : MonoBehaviour
{
    public const float DefaultProximityPadding = UIWindowDragHandler.DefaultProximityPadding;

    [Header("Proximity Reveal (opt-in)")]
    [Tooltip("false = 핸들 상시 히트(Inventory/Status). true = 근접 시에만 표시.")]
    [SerializeField] bool _enabled;

    [Tooltip("창 Rect 가장자리 판정 거리(로컬 px).")]
    [SerializeField] float _proximityPadding = DefaultProximityPadding;

    [Tooltip("선택. 헤더 드래그 중 리사이즈 핸들 숨김 / 리사이즈 중 헤더 억제용.")]
    [SerializeField] UIWindowDragHandler _dragHeader;

    [SerializeField] RectTransform _window;
    [SerializeField] Canvas _canvas;

    UIWindowResizeHandler[] _handlers;
    bool _initialized;
    bool _resizeHandlesActive = true;

    public bool IsProximityEnabled => _enabled;

    void Awake() => EnsureReady(resolveDepsIfMissing: true);

#if UNITY_EDITOR
    void OnValidate() => _proximityPadding = Mathf.Max(0f, _proximityPadding);
#endif

    public void Initialize(RectTransform window, Canvas canvas, float proximityPadding)
    {
        if (window != null)
            _window = window;
        if (canvas != null)
            _canvas = canvas;
        _proximityPadding = Mathf.Max(0f, proximityPadding);
        EnsureReady(resolveDepsIfMissing: true);
    }

    public void SetDragHeader(UIWindowDragHandler dragHeader) =>
        _dragHeader = dragHeader;

    public void SetResizeHandlers(UIWindowResizeHandler[] handlers) =>
        _handlers = handlers;

    /// <summary>옵트인. false면 LateUpdate 중단(기존 상시 히트 UX).</summary>
    public void SetProximityEnabled(bool enabled)
    {
        _enabled = enabled;
        this.enabled = enabled;
        if (!_initialized)
            return;

        if (!_enabled)
        {
            SyncHeaderSuppress(false);
            return;
        }

        HideResizeHandlesOnly();
        SyncHeaderSuppress(false);
    }

    /// <summary>리사이즈 핸들만 ON/OFF.</summary>
    public void SetResizeHandlesActive(bool active)
    {
        _resizeHandlesActive = active;
        if (!active)
            HideResizeHandlesOnly();
    }

    void OnDisable() => SyncHeaderSuppress(false);

    void LateUpdate()
    {
        if (!_enabled || !_initialized || _window == null)
            return;

        if (_dragHeader != null && _dragHeader.IsDragging)
        {
            HideResizeHandlesOnly();
            SyncHeaderSuppress(false);
            return;
        }

        UIWindowResizeHandler dragging = FindDragging();
        if (dragging != null)
        {
            SyncHeaderSuppress(true);
            if (_resizeHandlesActive)
                ApplyResizeReveal(dragging.Edge);
            else
                HideResizeHandlesOnly();
            return;
        }

        SyncHeaderSuppress(false);

        if (!_resizeHandlesActive)
        {
            HideResizeHandlesOnly();
            return;
        }

        if (!TryGetPointerLocal(out Vector2 local))
        {
            HideResizeHandlesOnly();
            return;
        }

        if (TryResolveNearEdge(local, out WindowResizeEdge edge))
            ApplyResizeReveal(edge);
        else
            HideResizeHandlesOnly();
    }

    void EnsureReady(bool resolveDepsIfMissing)
    {
        if (resolveDepsIfMissing)
        {
            if (_window == null)
                _window = transform as RectTransform;

            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();

            if (_handlers == null || _handlers.Length == 0)
            {
                UIWindowResizeHandles host = GetComponent<UIWindowResizeHandles>();
                if (host != null)
                    _handlers = host.Handlers;
            }

            if (_dragHeader == null)
                _dragHeader = GetComponentInChildren<UIWindowDragHandler>(true);
        }

        _initialized = _window != null;

        if (_handlers == null || _handlers.Length == 0)
        {
            Debug.LogError(
                "[UIWindowResizeProximity] Resize handlers not assigned. " +
                "Add UIWindowResizeHandles on the same GameObject.",
                this);
            return;
        }

        if (!_enabled)
            return;

        HideResizeHandlesOnly();
        SyncHeaderSuppress(false);
    }

    void SyncHeaderSuppress(bool suppressed)
    {
        if (_dragHeader != null)
            _dragHeader.SetProximitySuppressed(suppressed);
    }

    UIWindowResizeHandler FindDragging()
    {
        if (_handlers == null)
            return null;

        for (int i = 0; i < _handlers.Length; i++)
        {
            UIWindowResizeHandler h = _handlers[i];
            if (h != null && h.IsDragging)
                return h;
        }

        return null;
    }

    void ApplyResizeReveal(WindowResizeEdge edge)
    {
        if (_handlers == null)
            return;

        for (int i = 0; i < _handlers.Length; i++)
        {
            UIWindowResizeHandler h = _handlers[i];
            if (h == null)
                continue;
            h.SetVisualActive(h.Edge == edge);
        }
    }

    void HideResizeHandlesOnly()
    {
        if (_handlers == null)
            return;

        for (int i = 0; i < _handlers.Length; i++)
        {
            if (_handlers[i] != null)
                _handlers[i].SetVisualActive(false);
        }
    }

    bool TryGetPointerLocal(out Vector2 local)
    {
        local = default;
        Vector2 screen = ResolvePointerScreen();
        Camera cam = ResolveCamera();
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _window,
            screen,
            cam,
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

    bool TryResolveNearEdge(Vector2 local, out WindowResizeEdge edge)
    {
        edge = WindowResizeEdge.BottomRight;
        Rect r = _window.rect;
        float pad = _proximityPadding;

        if (!IsNearOrInsideWindow(local))
            return false;

        bool nearLeft = Mathf.Abs(local.x - r.xMin) <= pad;
        bool nearRight = Mathf.Abs(local.x - r.xMax) <= pad;
        bool nearBottom = Mathf.Abs(local.y - r.yMin) <= pad;
        bool nearTop = Mathf.Abs(local.y - r.yMax) <= pad;

        if (!nearLeft && !nearRight && !nearBottom && !nearTop)
            return false;

        if (nearLeft && nearTop)
        {
            edge = WindowResizeEdge.TopLeft;
            return true;
        }

        if (nearRight && nearTop)
        {
            edge = WindowResizeEdge.TopRight;
            return true;
        }

        if (nearLeft && nearBottom)
        {
            edge = WindowResizeEdge.BottomLeft;
            return true;
        }

        if (nearRight && nearBottom)
        {
            edge = WindowResizeEdge.BottomRight;
            return true;
        }

        if (nearLeft)
        {
            edge = WindowResizeEdge.Left;
            return true;
        }

        if (nearRight)
        {
            edge = WindowResizeEdge.Right;
            return true;
        }

        if (nearTop)
        {
            edge = WindowResizeEdge.Top;
            return true;
        }

        edge = WindowResizeEdge.Bottom;
        return true;
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
}
