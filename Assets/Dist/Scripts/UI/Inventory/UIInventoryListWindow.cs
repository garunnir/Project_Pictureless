// ============================================================
// UIInventoryListWindow — 인벤 리스트+사이드바 단일 View
// ============================================================

using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum InventoryWindowMode
{
    PlayerOnly,
    NearbyOnly
}

public sealed class UIInventoryListWindow : MonoBehaviour
{
    [Required, SerializeField] UIItemListView _listView;
    [Required, SerializeField] UIContainerSidebar _sidebar;
    [SerializeField] RectTransform _listArea;
    [SerializeField] RectTransform _sidebarArea;
    [SerializeField] InventoryWindowDragHandler _windowDragHandler;
    [SerializeField] TMP_Text _headerTitle;

    InventorySession _session;
    InventoryContainer _selectedContainer;
    InventoryWindowMode _mode = InventoryWindowMode.NearbyOnly;
    IInventoryItemDragHost _dragHost;
    bool _dragConfigured;

    readonly List<InventoryContainer> _filteredSidebar = new();
    readonly FixedContainerCapacityPolicy _nestedContainerPolicy = new();

    public bool IsVisible => gameObject.activeSelf;
    public RectTransform WindowRect => transform as RectTransform;
    public InventorySession Session => _session;
    public InventoryContainer SelectedContainer => _selectedContainer;
    public InventoryWindowMode Mode => _mode;
    public UIItemListView ListView => _listView;

    public void SetHeaderTitle(string title)
    {
        if (_headerTitle != null)
            _headerTitle.text = title;
    }

    public void ConfigureWindowChrome(Canvas rootCanvas, Vector2 minSize, Vector2 maxSize)
    {
        if (_windowDragHandler == null)
            _windowDragHandler = GetComponentInChildren<InventoryWindowDragHandler>(true);

        _windowDragHandler?.Initialize(WindowRect, rootCanvas);

        InventoryWindowResizeHandler[] resizeHandlers =
            GetComponentsInChildren<InventoryWindowResizeHandler>(true);
        for (int i = 0; i < resizeHandlers.Length; i++)
            resizeHandlers[i].Initialize(WindowRect, rootCanvas, minSize, maxSize);

        if (WindowRect != null && rootCanvas != null)
            WindowRect.sizeDelta = InventoryWindowLayout.ClampSize(WindowRect.sizeDelta, rootCanvas);
    }

    public void ConfigureWindowDrag(Canvas rootCanvas) =>
        ConfigureWindowChrome(
            rootCanvas,
            new Vector2(InventoryWindowLayout.MinWidth, InventoryWindowLayout.MinHeight),
            InventoryWindowLayout.GetMaxSize(rootCanvas));

    public void ConfigureDragAndDrop(IInventoryItemDragHost dragHost, Canvas rootCanvas)
    {
        if (_listView == null)
            return;

        _dragHost = dragHost;
        EnsureSidebarRaycastTarget();

        if (!_dragConfigured)
        {
            ScrollRect scroll = _listView.GetComponentInChildren<ScrollRect>();
            RectTransform viewport = scroll != null ? scroll.viewport : null;
            if (viewport == null)
            {
                Debug.LogError("[UIInventoryListWindow] ScrollRect viewport missing on inventory window prefab.", this);
            }
            else
            {
                if (!viewport.TryGetComponent(out UIInventoryListDropZone dropZone))
                {
                    Debug.LogError("[UIInventoryListWindow] UIInventoryListDropZone missing on viewport prefab.", viewport);
                }
                else
                {
                    dropZone.Bind(this);
                }

                if (!viewport.TryGetComponent(out InventoryListMarqueeSelector marquee))
                {
                    Debug.LogError("[UIInventoryListWindow] InventoryListMarqueeSelector missing on viewport prefab.", viewport);
                }
                else
                {
                    RectTransform selectionRect = FindMarqueeRect(viewport);
                    if (selectionRect == null)
                    {
                        Debug.LogError("[UIInventoryListWindow] MarqueeSelection child missing under viewport prefab.", viewport);
                    }
                    else
                    {
                        marquee.Bind(_listView, rootCanvas, selectionRect);
                    }
                }

                if (!viewport.TryGetComponent(out Image viewportImage))
                {
                    Debug.LogError("[UIInventoryListWindow] Image missing on viewport prefab.", viewport);
                }
                else
                {
                    viewportImage.color = new Color(0f, 0f, 0f, 0f);
                    viewportImage.raycastTarget = true;
                }
            }

            _dragConfigured = true;
        }

        _listView.Configure(_session, dragHost);
    }

    void EnsureSidebarRaycastTarget()
    {
        if (_sidebarArea == null)
            return;

        if (!_sidebarArea.TryGetComponent(out Image image))
        {
            Debug.LogError("[UIInventoryListWindow] Image missing on sidebar area prefab.", _sidebarArea);
            return;
        }

        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;
    }

    static RectTransform FindMarqueeRect(RectTransform parent)
    {
        Transform existing = parent.Find("MarqueeSelection");
        return existing != null ? existing as RectTransform : null;
    }

    public void Initialize(
        InventorySession session,
        InventoryWindowMode mode,
        InventoryContainer initialFocus)
    {
        _session = session;
        _mode = mode;
        ApplyModeLayout();

        if (_mode == InventoryWindowMode.PlayerOnly && initialFocus != null)
            _selectedContainer = initialFocus;
        else if (_mode == InventoryWindowMode.NearbyOnly)
            SyncNearbySelectionFromCoordinator(initialFocus, refreshList: false);
        else
            SelectContainer(initialFocus, refreshList: false);

        if (_dragHost != null)
            _listView?.Configure(_session, _dragHost);

        RefreshAll();
    }

    public void OnSidebarChanged()
    {
        if (_session == null)
            return;

        EnsureSelectedContainerForSidebar();
        RefreshSidebarAndSelection();
        RefreshListOnly();
    }

    public void ApplyActiveLootContainer(InventoryContainer container)
    {
        if (_mode != InventoryWindowMode.NearbyOnly)
            return;

        _selectedContainer = container;
        RefreshSidebarAndSelection();
        RefreshListOnly();
    }

    public void OnStacksChanged()
    {
        if (_session == null)
            return;

        if (_mode == InventoryWindowMode.PlayerOnly)
        {
            if (_selectedContainer == null)
                ResolvePlayerContainer();

            ApplyModeLayout();
            RefreshSidebarAndSelection();
        }

        RefreshListOnly();
    }

    public void OnSessionChanged()
    {
        OnSidebarChanged();
        OnStacksChanged();
    }

    public bool AddContainer(InventoryContainer container) =>
        _session != null && _session.TryAddSidebarContainer(container);

    public bool RemoveContainer(string instanceId) =>
        _session != null && _session.TryRemoveSidebarContainer(instanceId);

    public void SelectContainer(InventoryContainer container) =>
        SelectContainer(container, refreshList: true);

    void SelectContainer(InventoryContainer container, bool refreshList)
    {
        if (container == null)
            return;

        if (_mode == InventoryWindowMode.NearbyOnly &&
            container.InstanceId == PlayerInventoryHost.DefaultInstanceId)
            return;

        if (_mode == InventoryWindowMode.NearbyOnly)
        {
            if (!ContainsSidebarContainer(GetSidebarContainersForMode(), container.InstanceId))
                return;

            PlayerInventoryRuntime.Active?.LootProximity?.RequestActiveContainer(container);
            return;
        }

        _selectedContainer = container;

        if (refreshList)
            RefreshListOnly();
        else
            RefreshSidebarSelectionOnly();
    }

    public void SelectContainer(string instanceId)
    {
        if (_session == null || string.IsNullOrEmpty(instanceId))
            return;

        IReadOnlyList<InventoryContainer> sidebar = GetSidebarContainersForMode();
        for (int i = 0; i < sidebar.Count; i++)
        {
            if (sidebar[i].InstanceId == instanceId)
            {
                SelectContainer(sidebar[i]);
                return;
            }
        }
    }

    public void RefreshAll()
    {
        RefreshSidebarAndSelection();
        RefreshListOnly();
    }

    public void RefreshListOnly()
    {
        _listView?.Bind(_selectedContainer);
    }

    public void RefreshSidebarAndSelection()
    {
        if (ShouldShowSidebar())
        {
            IReadOnlyList<InventoryContainer> sidebar = GetSidebarContainersForMode();
            string selectedId = _selectedContainer != null ? _selectedContainer.InstanceId : string.Empty;
            _sidebar.Sync(sidebar, selectedId, SelectContainer, _dragHost, this, _session);
        }
        else
        {
            _sidebar.ClearSlots();
        }
    }

    void RefreshSidebarSelectionOnly()
    {
        if (!ShouldShowSidebar())
            return;

        string selectedId = _selectedContainer != null ? _selectedContainer.InstanceId : string.Empty;
        _sidebar.UpdateSelection(selectedId);
    }

    void SyncNearbySelectionFromCoordinator(InventoryContainer preferred, bool refreshList)
    {
        LootProximityCoordinator coordinator = PlayerInventoryRuntime.Active?.LootProximity;
        if (coordinator == null)
            return;

        if (preferred != null &&
            ContainsSidebarContainer(GetSidebarContainersForMode(), preferred.InstanceId))
            coordinator.RequestActiveContainer(preferred);

        _selectedContainer = coordinator.ActiveContainer;

        if (refreshList)
            RefreshListOnly();
        else
            RefreshSidebarSelectionOnly();
    }

    void EnsureSelectedContainerForSidebar()
    {
        IReadOnlyList<InventoryContainer> sidebar = GetSidebarContainersForMode();

        if (_mode == InventoryWindowMode.PlayerOnly)
        {
            if (_selectedContainer == null)
                ResolvePlayerContainer();
            else if (!ContainsSidebarContainer(sidebar, _selectedContainer.InstanceId))
                _selectedContainer = GetPlayerBodyContainer() ?? _selectedContainer;
        }
        else
        {
            LootProximityCoordinator coordinator = PlayerInventoryRuntime.Active?.LootProximity;
            InventoryContainer active = coordinator?.ActiveContainer;

            if (active != null && ContainsSidebarContainer(sidebar, active.InstanceId))
                _selectedContainer = active;
            else if (_selectedContainer == null ||
                     !ContainsSidebarContainer(sidebar, _selectedContainer.InstanceId))
                _selectedContainer = sidebar.Count > 0 ? sidebar[0] : null;
        }
    }

    void ResolvePlayerContainer()
    {
        if (_session == null)
            return;

        IReadOnlyList<InventoryContainer> all = _session.GetSidebarContainers();
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].InstanceId == PlayerInventoryHost.DefaultInstanceId)
            {
                _selectedContainer = all[i];
                return;
            }
        }

        PlayerInventoryRuntime runtime = PlayerInventoryRuntime.Active;
        if (runtime?.Host?.Container != null)
            _selectedContainer = runtime.Host.Container;
    }

    void ApplyModeLayout()
    {
        bool showSidebar = ShouldShowSidebar();

        if (_sidebarArea != null)
            _sidebarArea.gameObject.SetActive(showSidebar);
        else if (_sidebar != null)
            _sidebar.gameObject.SetActive(showSidebar);

        if (_listArea != null)
        {
            _listArea.offsetMax = new Vector2(showSidebar ? -120f : -10f, -(InventoryWindowLayout.HeaderHeight + 10f));
            LayoutRebuilder.ForceRebuildLayoutImmediate(_listArea);
        }
    }

    bool ShouldShowSidebar() =>
        _mode == InventoryWindowMode.NearbyOnly ||
        (_mode == InventoryWindowMode.PlayerOnly && HasPlayerBagTabs());

    bool HasPlayerBagTabs()
    {
        InventoryContainer body = GetPlayerBodyContainer();
        if (body == null)
            return false;

        for (int i = 0; i < body.Stacks.Count; i++)
        {
            ItemStack stack = body.Stacks[i];
            if (stack?.Item != null && stack.Item.IsContainer)
                return true;
        }

        return false;
    }

    InventoryContainer GetPlayerBodyContainer()
    {
        PlayerInventoryRuntime runtime = PlayerInventoryRuntime.Active;
        return runtime?.Host?.Container;
    }

    IReadOnlyList<InventoryContainer> GetSidebarContainersForMode()
    {
        _filteredSidebar.Clear();

        if (_session == null)
            return _filteredSidebar;

        if (_mode == InventoryWindowMode.PlayerOnly)
        {
            InventoryContainer body = GetPlayerBodyContainer();
            if (body == null || !HasPlayerBagTabs())
                return _filteredSidebar;

            _filteredSidebar.Add(body);
            for (int i = 0; i < body.Stacks.Count; i++)
            {
                ItemStack stack = body.Stacks[i];
                if (stack?.Item == null || !stack.Item.IsContainer)
                    continue;

                if (!stack.TryEnsureNested(_nestedContainerPolicy) || stack.Nested == null)
                    continue;

                _filteredSidebar.Add(stack.Nested);
            }

            return _filteredSidebar;
        }

        IReadOnlyList<InventoryContainer> all = _session.GetSidebarContainers();
        PlayerInventoryRuntime runtime = PlayerInventoryRuntime.Active;
        for (int i = 0; i < all.Count; i++)
        {
            InventoryContainer container = all[i];
            if (container == null)
                continue;

            if (container.InstanceId == PlayerInventoryHost.DefaultInstanceId)
                continue;

            if (runtime != null && !runtime.IsWorldLootContainer(container.InstanceId))
                continue;

            _filteredSidebar.Add(container);
        }

        return _filteredSidebar;
    }

    static bool ContainsSidebarContainer(IReadOnlyList<InventoryContainer> sidebar, string instanceId)
    {
        for (int i = 0; i < sidebar.Count; i++)
        {
            if (sidebar[i].InstanceId == instanceId)
                return true;
        }

        return false;
    }
}
