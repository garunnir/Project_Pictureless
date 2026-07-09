// ============================================================
// UIContainerSidebar — Session 사이드바 슬롯 목록
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class UIContainerSidebar : MonoBehaviour
{
    [SerializeField] RectTransform _slotRoot;
    [SerializeField] UIContainerSlot _slotPrefab;

    readonly List<UIContainerSlot> _slots = new();
    readonly Dictionary<string, UIContainerSlot> _slotsById = new();
    string _lastSelectedId = string.Empty;

    public void Rebuild(
        IReadOnlyList<InventoryContainer> containers,
        string selectedInstanceId,
        Action<InventoryContainer> onSelected,
        IInventoryItemDragHost dragHost,
        UIInventoryListWindow window,
        InventorySession session) =>
        Sync(containers, selectedInstanceId, onSelected, dragHost, window, session);

    public void Sync(
        IReadOnlyList<InventoryContainer> containers,
        string selectedInstanceId,
        Action<InventoryContainer> onSelected,
        IInventoryItemDragHost dragHost,
        UIInventoryListWindow window,
        InventorySession session)
    {
        if (_slotPrefab == null || _slotRoot == null || containers == null)
        {
            ClearSlots();
            return;
        }

        var desiredIds = new HashSet<string>();
        for (int i = 0; i < containers.Count; i++)
        {
            if (containers[i] != null)
                desiredIds.Add(containers[i].InstanceId);
        }

        for (int i = _slots.Count - 1; i >= 0; i--)
        {
            UIContainerSlot slot = _slots[i];
            if (slot == null)
            {
                _slots.RemoveAt(i);
                continue;
            }

            string slotId = slot.ContainerInstanceId;
            if (!desiredIds.Contains(slotId))
                RemoveSlotAt(i);
        }

        for (int i = 0; i < containers.Count; i++)
        {
            InventoryContainer container = containers[i];
            if (container == null)
                continue;

            if (!_slotsById.ContainsKey(container.InstanceId))
                AddSlot(container, onSelected, dragHost, window, session);
            else
                _slotsById[container.InstanceId].Bind(
                    container,
                    container.InstanceId == selectedInstanceId,
                    onSelected,
                    dragHost,
                    window,
                    session);
        }

        ReorderSlots(containers);
        UpdateSelection(selectedInstanceId, force: true);
    }

    public void UpdateSelection(string selectedInstanceId, bool force = false)
    {
        if (!force && _lastSelectedId == selectedInstanceId)
            return;

        for (int i = 0; i < _slots.Count; i++)
        {
            UIContainerSlot slot = _slots[i];
            if (slot == null)
                continue;

            slot.SetSelected(slot.ContainerInstanceId == selectedInstanceId);
        }

        _lastSelectedId = selectedInstanceId;
    }

    public void ClearSlots()
    {
        for (int i = _slots.Count - 1; i >= 0; i--)
            RemoveSlotAt(i);

        _lastSelectedId = string.Empty;
    }

    public void ClearDropHovers()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i] != null)
                _slots[i].SetDropHover(false);
        }
    }

    void AddSlot(
        InventoryContainer container,
        Action<InventoryContainer> onSelected,
        IInventoryItemDragHost dragHost,
        UIInventoryListWindow window,
        InventorySession session)
    {
        UIContainerSlot slot = Instantiate(_slotPrefab, _slotRoot);
        slot.Bind(container, false, onSelected, dragHost, window, session);
        _slots.Add(slot);
        _slotsById[container.InstanceId] = slot;
    }

    void RemoveSlotAt(int index)
    {
        UIContainerSlot slot = _slots[index];
        if (slot != null)
        {
            _slotsById.Remove(slot.ContainerInstanceId);
            Destroy(slot.gameObject);
        }

        _slots.RemoveAt(index);
    }

    void ReorderSlots(IReadOnlyList<InventoryContainer> containers)
    {
        var ordered = new List<UIContainerSlot>(containers.Count);
        for (int i = 0; i < containers.Count; i++)
        {
            InventoryContainer container = containers[i];
            if (container == null)
                continue;

            if (!_slotsById.TryGetValue(container.InstanceId, out UIContainerSlot slot) || slot == null)
                continue;

            slot.transform.SetSiblingIndex(ordered.Count);
            ordered.Add(slot);
        }

        _slots.Clear();
        _slots.AddRange(ordered);
    }
}
