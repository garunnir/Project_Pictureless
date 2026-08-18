// ============================================================
// HudLayoutParticipant — HUD 루트 레이아웃 편집 + 저장 (HUD 레이어 전용)
// ============================================================

using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class HudLayoutParticipant : MonoBehaviour
{
    [SerializeField] string _participantId;
    [SerializeField] RectTransform _window;
    [SerializeField] UIWindowDragHandler _headerDrag;
    [SerializeField] UIWindowDragHandler _layoutDrag;
    [SerializeField] UIWindowChromeBar _chromeBar;
    [SerializeField] UIWindowResizeHandles _resizeHandles;
    [SerializeField] UIWindowResizeProximity _resizeProximity;
    [SerializeField] Canvas _canvas;
    [SerializeField] Vector2 _minSize = Vector2.zero;
    [SerializeField] Vector2 _maxSize = Vector2.zero;

    Vector2 _lastSavedPos;
    Vector2 _lastSavedSize;
    bool _hasLastSaved;
    bool _eligible;

    public string ParticipantId => _participantId;

    void Awake()
    {
        if (_window == null)
            _window = transform as RectTransform;
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        _eligible = IsUnderHudLayer();
        if (!_eligible)
        {
            enabled = false;
            return;
        }

        ApplyStoredLayoutIfAny();
        HudPopupVisibility.Register(this);

        if (!HudPopupVisibility.IsVisible(_participantId))
            gameObject.SetActive(false);
    }

    void OnDestroy() => HudPopupVisibility.Unregister(this);

    void Start()
    {
        if (_eligible)
            ApplyEditMode(HudLayoutEdit.IsActive);
    }

    void OnEnable()
    {
        if (!_eligible)
            return;

        HudLayoutEdit.Changed += OnEditModeChanged;
        HudPopupVisibility.Changed += OnPopupVisibilityChanged;
        ApplyEditMode(HudLayoutEdit.IsActive);
    }

    void OnDisable()
    {
        HudLayoutEdit.Changed -= OnEditModeChanged;
        HudPopupVisibility.Changed -= OnPopupVisibilityChanged;
    }

    void LateUpdate()
    {
        if (!_eligible || !HudLayoutEdit.IsActive || _window == null)
            return;

        Vector2 pos = _window.anchoredPosition;
        Vector2 size = _window.sizeDelta;
        if (_hasLastSaved &&
            Mathf.Approximately(pos.x, _lastSavedPos.x) &&
            Mathf.Approximately(pos.y, _lastSavedPos.y) &&
            Mathf.Approximately(size.x, _lastSavedSize.x) &&
            Mathf.Approximately(size.y, _lastSavedSize.y))
            return;

        _lastSavedPos = pos;
        _lastSavedSize = size;
        _hasLastSaved = true;
        HudLayoutStore.Save(_participantId, pos, size);
    }

    public void Wire(
        string participantId,
        UIWindowDragHandler headerDrag,
        UIWindowDragHandler layoutDrag,
        UIWindowChromeBar chromeBar,
        UIWindowResizeHandles resizeHandles,
        UIWindowResizeProximity resizeProximity,
        Canvas canvas)
    {
        _participantId = participantId;
        _headerDrag = headerDrag;
        _layoutDrag = layoutDrag;
        _chromeBar = chromeBar;
        _resizeHandles = resizeHandles;
        _resizeProximity = resizeProximity;
        if (canvas != null)
            _canvas = canvas;
    }

    public void ApplyStoredVisibility()
    {
        if (!_eligible)
            return;

        bool visible = HudPopupVisibility.IsVisible(_participantId);
        if (gameObject.activeSelf == visible)
            return;

        gameObject.SetActive(visible);
        if (visible)
            ApplyEditMode(HudLayoutEdit.IsActive);
    }

    void OnEditModeChanged()
    {
        if (_eligible && gameObject.activeSelf)
            ApplyEditMode(HudLayoutEdit.IsActive);
    }

    void OnPopupVisibilityChanged() => ApplyStoredVisibility();

    bool IsUnderHudLayer()
    {
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null)
            return false;
        if (!_canvas.TryGetComponent(out UICanvasLayerHost host))
            return true;

        Transform hudRoot = host.GetLayerRoot(UICanvasLayer.HUD);
        return hudRoot != null && transform.IsChildOf(hudRoot);
    }

    void ApplyStoredLayoutIfAny()
    {
        if (_window == null || string.IsNullOrEmpty(_participantId))
            return;
        if (!HudLayoutStore.TryLoad(_participantId, out Vector2 pos, out Vector2 size))
            return;

        _window.anchoredPosition = pos;
        _window.sizeDelta = size;
        _lastSavedPos = pos;
        _lastSavedSize = size;
        _hasLastSaved = true;
    }

    void ApplyEditMode(bool editActive)
    {
        if (editActive)
            EnterEditMode();
        else
            ApplyNormalMode();
    }

    void ApplyNormalMode()
    {
        _layoutDrag?.SetVisualActive(false);
        if (_chromeBar != null)
            _chromeBar.gameObject.SetActive(false);

        if (_headerDrag != null)
        {
            _headerDrag.SetProximityRevealEnabled(false);
            _headerDrag.SetProximitySuppressed(true);
            _headerDrag.SetVisualActive(false);
        }

        _resizeProximity?.SetProximityEnabled(false);
        _resizeProximity?.SetResizeHandlesActive(false);
        _resizeHandles?.SetHandlesActive(false);
    }

    void EnterEditMode()
    {
        if (_headerDrag != null)
        {
            _headerDrag.SetProximityRevealEnabled(false);
            _headerDrag.SetProximitySuppressed(false);
            _headerDrag.SetVisualActive(true);
        }

        if (_chromeBar != null)
            _chromeBar.gameObject.SetActive(true);

        if (_layoutDrag != null)
        {
            _layoutDrag.Initialize(_window, _canvas);
            _layoutDrag.SetProximityRevealEnabled(false);
            _layoutDrag.SetVisualActive(true);
        }

        if (_resizeHandles != null)
        {
            _resizeHandles.SetProximityReveal(false);
            Vector2 minSize = new(
                Mathf.Max(0f, _minSize.x),
                Mathf.Max(0f, _minSize.y));
            Vector2 maxSize = (_maxSize.x <= 0f || _maxSize.y <= 0f)
                ? new Vector2(float.MaxValue, float.MaxValue)
                : _maxSize;
            _resizeHandles.Initialize(
                _window,
                _canvas,
                minSize,
                maxSize);
            _resizeHandles.SetHandlesActive(true);
            UIWindowResizeHandler[] handlers = _resizeHandles.Handlers;
            for (int i = 0; i < handlers.Length; i++)
                handlers[i]?.SetVisualActive(true);
        }

        _resizeProximity?.SetProximityEnabled(false);
        _resizeProximity?.SetResizeHandlesActive(true);
    }

}
