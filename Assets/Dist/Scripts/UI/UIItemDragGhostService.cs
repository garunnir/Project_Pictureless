// ============================================================
// UIItemDragGhostService — 캔버스 TopMost 공용 아이템 드래그 고스트
// ============================================================

using UnityEngine;

[RequireComponent(typeof(Canvas))]
public sealed class UIItemDragGhostService : MonoBehaviour
{
    const string RuntimeInstanceName = "InventoryDragGhost";

    [SerializeField] UIInventoryDragGhost _prefab;
    [SerializeField] UIInventoryDragGhost _instance;
    [SerializeField] Canvas _canvas;
    [SerializeField] UICanvasLayerHost _layerHost;

    public static bool TryGet(Canvas canvas, out UIItemDragGhostService service)
    {
        service = null;
        if (canvas == null)
            return false;

        return canvas.TryGetComponent(out service) && service != null;
    }

    public void EnsureReady()
    {
        if (_canvas == null)
            TryGetComponent(out _canvas);
        if (_layerHost == null && _canvas != null)
            _layerHost = _canvas.GetComponent<UICanvasLayerHost>();

        EnsureInstance();
    }

    public void Show(Sprite icon, int stackCount, Vector2 screenPosition)
    {
        EnsureReady();
        if (_instance == null)
            return;

        _instance.Show(icon, stackCount, screenPosition);
    }

    public void SetScreenPosition(Vector2 screenPosition)
    {
        EnsureReady();
        if (_instance == null)
            return;

        _instance.SetScreenPosition(screenPosition);
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
            _instance.EnsureReady(_canvas);
            return;
        }

        if (_prefab == null)
        {
            Debug.LogError(
                "[UIItemDragGhostService] Drag ghost prefab is not assigned. Run Dist/MCP/Inventory/Setup Canvas Overlays In Open Scene.",
                this);
            return;
        }

        Transform parent = _layerHost != null
            ? _layerHost.GetLayerRoot(UICanvasLayer.TopMost)
            : _canvas.transform;

        _instance = Instantiate(_prefab, parent);
        _instance.name = RuntimeInstanceName;
        _instance.EnsureReady(_canvas);
        _instance.Hide();
    }
}
