// ============================================================
// UIWindowResizeHandles — 창 루트에 부착 시 8방향 리사이즈 핸들 런타임 생성
// ============================================================

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[InfoBox(
    "창 루트에 부착하면 Awake에서 8방향 핸들을 만들고 리사이즈가 동작합니다. " +
    "Window/Canvas는 비우면 자신·부모에서 찾고, Initialize로 min/max·Canvas 주입도 가능합니다. " +
    "Proximity Reveal을 켜면 같은 GO의 UIWindowResizeProximity에 핸들을 자동 주입합니다.",
    InfoMessageType.Info)]
public sealed class UIWindowResizeHandles : MonoBehaviour
{
    public const float DefaultHandleWidth = 8f;
    public const float DefaultMinSize = 80f;
    public const float DefaultMaxSize = 4096f;

    /// <summary>코너 히트박스 = handleWidth × 이 비율 (현행 Time 14/8).</summary>
    const float CornerSizeFactor = 14f / 8f;

    static readonly Color AlwaysHitColor = new(1f, 1f, 1f, 0.02f);
    static readonly Color ProximityRevealColor = new(1f, 1f, 1f, 0.85f);

    [SerializeField, Min(1f)] float _handleWidth = DefaultHandleWidth;

    [SerializeField] RectTransform _window;
    [SerializeField] Canvas _canvas;

    [SerializeField] Vector2 _minSize = new(DefaultMinSize, DefaultMinSize);
    [SerializeField] Vector2 _maxSize = new(DefaultMaxSize, DefaultMaxSize);

    [Header("Proximity Reveal (opt-in)")]
    [Tooltip("true = CanvasGroup 숨김 + 같은 GO Proximity에 핸들 주입. false = 상시 투명 히트.")]
    [SerializeField] bool _proximityReveal;

    UIWindowResizeHandler[] _handlers;
    bool _handlesBuilt;

    public float HandleWidth => _handleWidth;
    public bool IsProximityReveal => _proximityReveal;

    public void SetHandleWidth(float width)
    {
        float w = Mathf.Max(1f, width);
        if (Mathf.Approximately(_handleWidth, w))
            return;
        _handleWidth = w;
        if (_handlesBuilt)
            RebuildHandles();
    }

    public UIWindowResizeHandler[] Handlers
    {
        get
        {
            EnsureHandles();
            return _handlers;
        }
    }

    void Awake() => EnsureReady(resolveCanvasIfMissing: true);

#if UNITY_EDITOR
    void OnValidate()
    {
        _handleWidth = Mathf.Max(1f, _handleWidth);
        if (_minSize.x < 1f)
            _minSize.x = 1f;
        if (_minSize.y < 1f)
            _minSize.y = 1f;
        if (_maxSize.x < _minSize.x)
            _maxSize.x = _minSize.x;
        if (_maxSize.y < _minSize.y)
            _maxSize.y = _minSize.y;
    }
#endif

    /// <summary>null 인자는 해당 필드를 유지합니다.</summary>
    public void Initialize(
        RectTransform window,
        Canvas canvas,
        Vector2 minSize,
        Vector2 maxSize)
    {
        if (window != null)
            _window = window;
        if (canvas != null)
            _canvas = canvas;
        _minSize = minSize;
        _maxSize = maxSize;
        EnsureReady(resolveCanvasIfMissing: true);
    }

    public void SetProximityReveal(bool enabled)
    {
        bool changed = _proximityReveal != enabled;
        _proximityReveal = enabled;
        if (!_handlesBuilt)
            return;

        if (changed)
            RebuildHandles();
        else
            WireProximityIfPresent();

        if (!_proximityReveal)
            SetHandlesActive(true);
    }

    public void SetHandlesActive(bool active)
    {
        EnsureHandles();
        if (_handlers == null)
            return;

        if (_proximityReveal)
        {
            for (int i = 0; i < _handlers.Length; i++)
                _handlers[i]?.SetVisualActive(false);

            UIWindowResizeProximity proximity = GetComponent<UIWindowResizeProximity>();
            proximity?.SetResizeHandlesActive(active);
            return;
        }

        for (int i = 0; i < _handlers.Length; i++)
        {
            if (_handlers[i] == null)
                continue;
            // AlwaysHit: SetVisualActive(true)는 revealed alpha로 덮어쓰므로 raycast만 토글.
            Image image = _handlers[i].GetComponent<Image>();
            if (image != null)
            {
                if (!active)
                {
                    _handlers[i].SetVisualActive(false);
                }
                else
                {
                    Color c = AlwaysHitColor;
                    image.color = c;
                    image.raycastTarget = true;
                }
            }
        }
    }

    void RebuildHandles()
    {
        if (_handlers != null)
        {
            for (int i = 0; i < _handlers.Length; i++)
            {
                if (_handlers[i] != null)
                    DestroyHandleGo(_handlers[i].gameObject);
            }
        }

        _handlers = null;
        _handlesBuilt = false;
        EnsureHandles();
        ApplyInitializeToHandlers();
        WireProximityIfPresent();
    }

    void EnsureReady(bool resolveCanvasIfMissing)
    {
        if (_window == null)
            _window = transform as RectTransform;

        if (resolveCanvasIfMissing && _canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        EnsureHandles();
        ApplyInitializeToHandlers();
        WireProximityIfPresent();
    }

    void EnsureHandles()
    {
        if (_handlesBuilt && _handlers != null && _handlers.Length == 8)
            return;

        // 프리팹에 남은 구 핸들은 Patch로 제거한다. 여기서는 런타임 배열만 소유.
        DestroyLegacyPrefabHandles();

        float edge = Mathf.Max(1f, _handleWidth);
        float corner = edge * CornerSizeFactor;
        Transform root = transform;

        _handlers = new[]
        {
            CreateHandle(root, "Area_ResizeHandle_Left", WindowResizeEdge.Left,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(edge, 0f)),
            CreateHandle(root, "Area_ResizeHandle_Right", WindowResizeEdge.Right,
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(edge, 0f)),
            CreateHandle(root, "Area_ResizeHandle_Top", WindowResizeEdge.Top,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, edge)),
            CreateHandle(root, "Area_ResizeHandle_Bottom", WindowResizeEdge.Bottom,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, edge)),
            CreateHandle(root, "Area_ResizeHandle_TopLeft", WindowResizeEdge.TopLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(corner, corner)),
            CreateHandle(root, "Area_ResizeHandle_TopRight", WindowResizeEdge.TopRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(corner, corner)),
            CreateHandle(root, "Area_ResizeHandle_BottomLeft", WindowResizeEdge.BottomLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(corner, corner)),
            CreateHandle(root, "Area_ResizeHandle_BottomRight", WindowResizeEdge.BottomRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(corner, corner)),
        };

        _handlesBuilt = true;
    }

    void DestroyLegacyPrefabHandles()
    {
        UIWindowResizeHandler[] existing =
            GetComponentsInChildren<UIWindowResizeHandler>(true);
        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] == null)
                continue;
            // 이 컴포넌트가 방금 만든 핸들은 _handlesBuilt 전에만 호출되므로 전부 레거시.
            DestroyHandleGo(existing[i].gameObject);
        }
    }

    static void DestroyHandleGo(GameObject go)
    {
        if (go == null)
            return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(go);
            return;
        }
#endif
        Destroy(go);
    }

    UIWindowResizeHandler CreateHandle(
        Transform parent,
        string name,
        WindowResizeEdge edge,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.layer = gameObject.layer;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = sizeDelta;

        Image image = go.GetComponent<Image>();
        image.color = _proximityReveal ? ProximityRevealColor : AlwaysHitColor;

        if (_proximityReveal)
        {
            CanvasGroup group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            image.raycastTarget = false;
        }
        else
        {
            image.raycastTarget = true;
        }

        UIWindowResizeHandler handler = go.AddComponent<UIWindowResizeHandler>();
        handler.SetEdge(edge);
        if (_proximityReveal)
            handler.SetRevealedAlpha(UIWindowResizeHandler.DefaultRevealedAlpha);

        return handler;
    }

    void ApplyInitializeToHandlers()
    {
        if (_handlers == null || _window == null)
            return;

        for (int i = 0; i < _handlers.Length; i++)
        {
            if (_handlers[i] == null)
                continue;
            _handlers[i].Initialize(_window, _canvas, _minSize, _maxSize);
        }
    }

    void WireProximityIfPresent()
    {
        UIWindowResizeProximity proximity = GetComponent<UIWindowResizeProximity>();
        if (proximity == null)
            return;

        proximity.SetResizeHandlers(Handlers);

        if (_window != null)
        {
            UIWindowDragHandler drag = GetComponentInChildren<UIWindowDragHandler>(true);
            if (drag != null)
                proximity.SetDragHeader(drag);

            proximity.Initialize(_window, _canvas, UIWindowResizeProximity.DefaultProximityPadding);
        }

        if (_proximityReveal)
            proximity.SetProximityEnabled(true);
    }
}
