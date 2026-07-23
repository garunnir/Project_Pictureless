// ============================================================
// UIWindowResizeProximity — 창 가장자리/헤더 근접 시 크롬 표시 (공용, 옵트인)
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 창 루트에 1개. 기본 비활성(옵트인).
/// 켜면 직렬화된 리사이즈 핸들·드래그 헤더를 포인터 근접 시에만 가시·히트한다.
/// Inventory/Status는 미부착 시 기존 UX 유지.
/// </summary>
[DisallowMultipleComponent]
public sealed class UIWindowResizeProximity : MonoBehaviour
{
    public const float DefaultProximityPadding = 12f;

    [Header("Proximity Reveal (opt-in)")]
    [Tooltip("false = 핸들 상시 히트(Inventory/Status). true = 근접 시에만 표시.")]
    [SerializeField] bool _enabled;

    [Tooltip("창 Rect 가장자리·헤더 판정 거리(로컬 px).")]
    [SerializeField] float _proximityPadding = DefaultProximityPadding;

    [SerializeField] UIWindowDragHandler _dragHeader;
    [SerializeField] UIWindowResizeHandler[] _handlers;

    RectTransform _window;
    Canvas _canvas;
    bool _initialized;
    bool _headerProximityActive = true;
    bool _resizeHandlesActive = true;

    public bool IsProximityEnabled => _enabled;

    public void Initialize(RectTransform window, Canvas canvas, float proximityPadding)
    {
        _window = window;
        _canvas = canvas;
        _proximityPadding = Mathf.Max(0f, proximityPadding);
        _initialized = true;

        if (_dragHeader == null)
            Debug.LogError(
                "[UIWindowResizeProximity] Drag header not assigned.",
                this);
        if (_handlers == null || _handlers.Length == 0)
            Debug.LogError(
                "[UIWindowResizeProximity] Resize handlers not assigned.",
                this);

        if (!_enabled)
            return;

        HideChrome();
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
            return;

        HideChrome();
    }

    /// <summary>드래그 헤더 근접 표시 ON/OFF. false면 헤더 숨김(드래그 불가).</summary>
    public void SetHeaderProximityActive(bool active)
    {
        _headerProximityActive = active;
        if (!active)
            SetHeaderVisible(false);
    }

    /// <summary>리사이즈 핸들만 ON/OFF. false여도 드래그 헤더 근접 표시는 유지.</summary>
    public void SetResizeHandlesActive(bool active)
    {
        _resizeHandlesActive = active;
        if (!active)
            HideResizeHandlesOnly();
    }

    void LateUpdate()
    {
        if (!_enabled || !_initialized || _window == null)
            return;

        if (_headerProximityActive && _dragHeader != null && _dragHeader.IsDragging)
        {
            SetHeaderVisible(true);
            if (_resizeHandlesActive)
                HideResizeHandlesOnly();
            return;
        }

        UIWindowResizeHandler dragging = FindDragging();
        if (dragging != null)
        {
            SetHeaderVisible(false);
            if (_resizeHandlesActive)
                ApplyResizeReveal(dragging.Edge);
            else
                HideResizeHandlesOnly();
            return;
        }

        if (!TryGetPointerLocal(out Vector2 local))
        {
            HideChrome();
            return;
        }

        bool nearWindow = IsNearOrInsideWindow(local);
        bool nearEdge = TryResolveNearEdge(local, out WindowResizeEdge edge);
        bool nearTop = IsNearTop(local);

        if (_headerProximityActive)
            SetHeaderVisible(nearWindow || nearTop);
        else
            SetHeaderVisible(false);

        if (!_resizeHandlesActive)
        {
            HideResizeHandlesOnly();
            return;
        }

        if (nearEdge)
            ApplyResizeReveal(edge);
        else
            HideResizeHandlesOnly();
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

    void SetHeaderVisible(bool visible)
    {
        if (_dragHeader != null)
            _dragHeader.SetVisualActive(visible);
    }

    void HideChrome()
    {
        SetHeaderVisible(false);
        HideResizeHandlesOnly();
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

    bool IsNearTop(Vector2 local)
    {
        Rect r = _window.rect;
        float pad = _proximityPadding;
        if (!IsNearOrInsideWindow(local))
            return false;
        return Mathf.Abs(local.y - r.yMax) <= pad;
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
