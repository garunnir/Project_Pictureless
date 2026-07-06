// ============================================================
// UIInventoryController — Session·Detector·인벤 창 제어 (I 토글)
// ============================================================

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInventoryHost))]
[RequireComponent(typeof(NearbyContainerDetector))]
public sealed class UIInventoryController : MonoBehaviour
{
    [Required, SerializeField] PlayerInventoryHost _host;
    [Required, SerializeField] NearbyContainerDetector _detector;
    [SerializeField] UIInventoryListWindow _primaryWindow;
    [SerializeField] UIInventoryListWindow _lootWindow;
    [SerializeField] Canvas _uiCanvas;
    [SerializeField] bool _seedDemoItems = true;

    InventorySession _session;
    bool _isOpen;

    public InventorySession Session => _session;

    void Awake()
    {
        EnsureWindows();
        _session = new InventorySession();
        _detector.Bind(_session);
        _session.SidebarChanged += OnSessionChanged;

        if (_primaryWindow != null)
            _primaryWindow.gameObject.SetActive(false);
        if (_lootWindow != null)
            _lootWindow.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (_session != null)
            _session.SidebarChanged -= OnSessionChanged;

        CloseInventory();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            ToggleInventory();
    }

    void OnValidate() => EnsureReferences();
    void Reset() => EnsureReferences();

    void EnsureReferences()
    {
        if (!_host) TryGetComponent(out _host);
        if (!_detector) TryGetComponent(out _detector);
        if (!_uiCanvas) _uiCanvas = FindAnyObjectByType<Canvas>();
    }

    void EnsureWindows()
    {
        if (_uiCanvas == null)
            _uiCanvas = FindAnyObjectByType<Canvas>();

        if (_uiCanvas == null)
        {
            Debug.LogError("[UIInventoryController] Canvas not found.", this);
            return;
        }

        if (_primaryWindow == null)
            _primaryWindow = InventoryUIFactory.CreateWindow(_uiCanvas.transform, "Grp_InventoryListWindow_Primary");

        if (_lootWindow == null)
        {
            _lootWindow = InventoryUIFactory.CreateWindow(_uiCanvas.transform, "Grp_InventoryListWindow_Loot");
            var lootRect = _lootWindow.GetComponent<RectTransform>();
            lootRect.anchoredPosition = new Vector2(380f, 0f);
        }
    }

    public void ToggleInventory()
    {
        if (_isOpen)
            CloseInventory();
        else
            OpenInventory();
    }

    public void OpenInventory()
    {
        if (_isOpen)
            return;

        if (_seedDemoItems)
            InventoryDemoSeeder.SeedIfEmpty(_host.Container);

        _host.RegisterToSession(_session);
        _detector.Activate();

        _primaryWindow.gameObject.SetActive(true);
        _primaryWindow.Initialize(_session, _host.Container);
        _isOpen = true;
    }

    public void CloseInventory()
    {
        if (!_isOpen)
            return;

        _detector.Deactivate();
        _host.UnregisterFromSession(_session);

        _primaryWindow.gameObject.SetActive(false);
        if (_lootWindow != null)
            _lootWindow.gameObject.SetActive(false);

        _isOpen = false;
    }

    public void OpenLoot(InventoryContainer focusContainer)
    {
        if (focusContainer == null)
            return;

        if (!_isOpen)
            OpenInventory();

        _session.TryAddSidebarContainer(focusContainer);
        InventoryDemoSeeder.SeedIfEmpty(focusContainer);

        if (_lootWindow != null)
        {
            _lootWindow.gameObject.SetActive(true);
            _lootWindow.Initialize(_session, focusContainer);
        }
        else
        {
            _primaryWindow.SelectContainer(focusContainer);
        }
    }

    void OnSessionChanged()
    {
        if (_primaryWindow != null && _primaryWindow.IsVisible)
            _primaryWindow.OnSessionChanged();

        if (_lootWindow != null && _lootWindow.IsVisible)
            _lootWindow.OnSessionChanged();
    }
}
