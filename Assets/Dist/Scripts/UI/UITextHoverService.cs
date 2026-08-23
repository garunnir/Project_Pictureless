// ============================================================
// UITextHoverService — 캔버스 TopMost 공용 텍스트 호버
// ============================================================

using UnityEngine;

[RequireComponent(typeof(Canvas))]
public sealed class UITextHoverService : MonoBehaviour
{
    const string RuntimeInstanceName = "TextHoverPanel";

    [SerializeField] UITextHoverPanel _prefab;
    [SerializeField] UITextHoverPanel _instance;
    [SerializeField] Canvas _canvas;
    [SerializeField] UICanvasLayerHost _layerHost;

    public static bool TryGet(Canvas canvas, out UITextHoverService service)
    {
        service = null;
        if (canvas == null)
            return false;

        return canvas.TryGetComponent(out service) && service != null;
    }

    public static bool TryShowNearAnchor(Canvas canvas, string body, RectTransform anchor)
    {
        return TryShowNearAnchor(canvas, body, anchor, UITextHoverPanel.DefaultStyle);
    }

    public static bool TryShowNearAnchor(
        Canvas canvas,
        string body,
        RectTransform anchor,
        UIHoverStyle style)
    {
        if (!TryGet(canvas, out UITextHoverService service) || service == null)
            return false;

        service.ShowNearAnchor(body, anchor, style);
        return true;
    }

    public static void HideOn(Canvas canvas)
    {
        if (TryGet(canvas, out UITextHoverService service) && service != null)
            service.Hide();
    }

    public void EnsureReady()
    {
        if (_canvas == null)
            TryGetComponent(out _canvas);
        if (_layerHost == null && _canvas != null)
            _layerHost = _canvas.GetComponent<UICanvasLayerHost>();

        EnsureInstance();
    }

    public void ShowNearAnchor(string body, RectTransform anchor)
    {
        ShowNearAnchor(body, anchor, UITextHoverPanel.DefaultStyle);
    }

    public void ShowNearAnchor(string body, RectTransform anchor, UIHoverStyle style)
    {
        EnsureReady();
        if (_instance == null)
            return;

        _instance.ShowNearAnchor(body, anchor, style);
    }

    public void Hide()
    {
        if (_instance == null)
            return;

        _instance.Hide();
    }

    void EnsureInstance()
    {
        if (_canvas == null)
            return;

        if (_instance != null)
        {
            _instance.Initialize(_canvas);
            return;
        }

        if (_prefab == null)
        {
            Debug.LogError(
                "[UITextHoverService] Prefab is not assigned. Run Dist/MCP/Inventory/Setup Canvas Overlays In Open Scene.",
                this);
            return;
        }

        Transform parent = _layerHost != null
            ? _layerHost.GetLayerRoot(UIHoverCanvasLayer.Layer)
            : _canvas.transform;

        _instance = Instantiate(_prefab, parent);
        _instance.name = RuntimeInstanceName;
        _instance.Initialize(_canvas);
        _instance.Hide();
    }
}
