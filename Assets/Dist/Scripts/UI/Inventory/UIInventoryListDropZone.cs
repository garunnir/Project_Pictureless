// ============================================================
// UIInventoryListDropZone — 리스트 영역 드롭 → MoveStacks
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UIInventoryListDropZone : MonoBehaviour, IDropHandler
{
    UIInventoryListWindow _window;

    public void Bind(UIInventoryListWindow window) => _window = window;

    public void OnDrop(PointerEventData eventData)
    {
        if (_window == null)
            return;

        InventoryContainer target = _window.SelectedContainer;
        if (LootAggregateHost.IsAggregateContainer(target))
            return;

        InventoryDragDrop.TryApplyTo(_window.Session, target);
    }
}
