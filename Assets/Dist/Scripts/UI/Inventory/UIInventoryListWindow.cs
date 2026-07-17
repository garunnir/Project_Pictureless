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
    [SerializeField] TMP_Text _weightText;
    [SerializeField] TMP_Text _volumeText;

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

        if (_selectedContainer != null)
            SetActiveContainer(_selectedContainer);
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
            SetActiveContainer(initialFocus, refreshList: false);
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
    }

    public void ApplyActiveLootContainer(InventoryContainer container)
    {
        if (_mode != InventoryWindowMode.NearbyOnly)
            return;

        SetActiveContainer(container);
        RefreshSidebarAndSelection();
    }

    public void OnStacksChanged()
    {
        if (_session == null)
            return;

        if (_mode == InventoryWindowMode.PlayerOnly)
        {
            if (_selectedContainer == null)
            {
                InventoryContainer resolved = ResolvePlayerContainer();
                if (resolved != null)
                    SetActiveContainer(resolved, refreshList: false);
            }

            ApplyModeLayout();
            RefreshSidebarAndSelection();
        }

        if (_selectedContainer != null)
            SetActiveContainer(_selectedContainer);
        else
            RefreshCapacityInfo();
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

        SetActiveContainer(container, refreshList);
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
        if (_selectedContainer != null)
            SetActiveContainer(_selectedContainer);
    }

    public void RefreshListOnly()
    {
        if (_selectedContainer == null)
            return;

        SetActiveContainer(_selectedContainer);
    }

    public void ClearSidebarDropHovers()
    {
        if (!ShouldShowSidebar())
            return;

        _sidebar.ClearDropHovers();
    }

    void SetActiveContainer(InventoryContainer container, bool refreshList = true)
    {
        _selectedContainer = container;
        ApplySidebarSelectionHighlight();
        RefreshCapacityInfo();

        if (!refreshList || InventoryDragState.IsDragging)
            return;

        _listView?.Bind(container);
    }

    void RefreshCapacityInfo()
    {
        if (_selectedContainer == null)
        {
            if (_weightText != null)
                _weightText.text = InventoryWindowLabels.EmptyWeight;
            if (_volumeText != null)
                _volumeText.text = InventoryWindowLabels.EmptyVolume;
            return;
        }

        float usedWeight = _selectedContainer.GetTotalWeight();
        float usedVolume = _selectedContainer.GetTotalVolume();
        float maxWeight = _selectedContainer.CapacityPolicy.GetMaxWeight(_selectedContainer);
        float maxVolume = _selectedContainer.CapacityPolicy.GetMaxVolume(_selectedContainer);

        if (_weightText != null)
            _weightText.text = InventoryWindowLabels.FormatWeightCapacity(usedWeight, maxWeight);
        if (_volumeText != null)
            _volumeText.text = InventoryWindowLabels.FormatVolumeCapacity(usedVolume, maxVolume);
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

    void ApplySidebarSelectionHighlight()
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

        SetActiveContainer(coordinator.ActiveContainer, refreshList);
    }

    void EnsureSelectedContainerForSidebar()
    {
        IReadOnlyList<InventoryContainer> sidebar = GetSidebarContainersForMode();

        if (_selectedContainer != null && ContainsSidebarContainer(sidebar, _selectedContainer.InstanceId))
            return;

        InventoryContainer fallback = null;

        if (_mode == InventoryWindowMode.PlayerOnly)
            fallback = GetPlayerBodyContainer() ?? ResolvePlayerContainer();
        else
        {
            LootProximityCoordinator coordinator = PlayerInventoryRuntime.Active?.LootProximity;
            InventoryContainer active = coordinator?.ActiveContainer;

            if (active != null && ContainsSidebarContainer(sidebar, active.InstanceId))
                fallback = active;
            else if (sidebar.Count > 0)
                fallback = sidebar[0];
        }

        if (fallback != null)
            SetActiveContainer(fallback);
        else
            _selectedContainer = null;
    }

    InventoryContainer ResolvePlayerContainer()
    {
        if (_session == null)
            return GetPlayerBodyContainer();

        IReadOnlyList<InventoryContainer> all = _session.GetSidebarContainers();
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].InstanceId == PlayerInventoryHost.DefaultInstanceId)
                return all[i];
        }

        return GetPlayerBodyContainer();
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
            // Prefer prefab sidebar geometry over hardcoded 120 — loot chrome is narrower.
            Vector2 offsetMax = _listArea.offsetMax;
            offsetMax.x = -ResolveListRightInset(showSidebar);
            _listArea.offsetMax = offsetMax;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_listArea);
        }
    }

    float ResolveListRightInset(bool showSidebar)
    {
        if (_sidebarArea == null)
        {
            Debug.LogError("[UIInventoryListWindow] Sidebar area missing; cannot resolve list inset from prefab.", this);
            return 0f;
        }

        // Right-anchored sidebar: offsetMin.x = -width, offsetMax.x = -chromeMargin.
        return showSidebar
            ? -_sidebarArea.offsetMin.x
            : -_sidebarArea.offsetMax.x;
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
            if (stack?.Item != null && stack.Item.is_container)
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
                if (stack?.Item == null || !stack.Item.is_container)
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
