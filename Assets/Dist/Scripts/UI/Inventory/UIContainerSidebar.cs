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

    public void Rebuild(
        IReadOnlyList<InventoryContainer> containers,
        string selectedInstanceId,
        Action<InventoryContainer> onSelected)
    {
        ClearSlots();

        if (_slotPrefab == null || _slotRoot == null || containers == null)
            return;

        for (int i = 0; i < containers.Count; i++)
        {
            InventoryContainer container = containers[i];
            UIContainerSlot slot = Instantiate(_slotPrefab, _slotRoot);
            bool selected = container.InstanceId == selectedInstanceId;
            slot.Bind(container, selected, onSelected);
            _slots.Add(slot);
        }
    }

    public void ClearSlots()
    {
        for (int i = _slots.Count - 1; i >= 0; i--)
        {
            if (_slots[i] != null)
                Destroy(_slots[i].gameObject);
        }

        _slots.Clear();
    }
}
