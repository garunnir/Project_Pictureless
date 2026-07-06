// ============================================================
// UIInventoryListWindow — 인벤 리스트+사이드바 단일 View
// ============================================================

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class UIInventoryListWindow : MonoBehaviour
{
    [Required, SerializeField] UIItemListView _listView;
    [Required, SerializeField] UIContainerSidebar _sidebar;

    InventorySession _session;
    InventoryContainer _selectedContainer;

    public bool IsVisible => gameObject.activeSelf;

    public void Initialize(InventorySession session, InventoryContainer initialFocus)
    {
        _session = session;
        SelectContainer(initialFocus);
        RefreshAll();
    }

    public void OnSessionChanged()
    {
        if (_session == null)
            return;

        if (_selectedContainer != null &&
            !ContainsSidebarContainer(_selectedContainer.InstanceId))
        {
            IReadOnlyList<InventoryContainer> sidebar = _session.GetSidebarContainers();
            _selectedContainer = sidebar.Count > 0 ? sidebar[0] : null;
        }

        RefreshAll();
    }

    public bool AddContainer(InventoryContainer container) =>
        _session != null && _session.TryAddSidebarContainer(container);

    public bool RemoveContainer(string instanceId) =>
        _session != null && _session.TryRemoveSidebarContainer(instanceId);

    public void SelectContainer(InventoryContainer container)
    {
        if (container == null)
            return;

        _selectedContainer = container;
        RefreshAll();
    }

    public void SelectContainer(string instanceId)
    {
        if (_session == null || string.IsNullOrEmpty(instanceId))
            return;

        IReadOnlyList<InventoryContainer> sidebar = _session.GetSidebarContainers();
        for (int i = 0; i < sidebar.Count; i++)
        {
            if (sidebar[i].InstanceId == instanceId)
            {
                SelectContainer(sidebar[i]);
                return;
            }
        }
    }

    void RefreshAll()
    {
        string selectedId = _selectedContainer != null ? _selectedContainer.InstanceId : string.Empty;
        _sidebar.Rebuild(_session?.GetSidebarContainers(), selectedId, SelectContainer);
        _listView.Bind(_selectedContainer);
    }

    bool ContainsSidebarContainer(string instanceId)
    {
        IReadOnlyList<InventoryContainer> sidebar = _session.GetSidebarContainers();
        for (int i = 0; i < sidebar.Count; i++)
        {
            if (sidebar[i].InstanceId == instanceId)
                return true;
        }

        return false;
    }
}
