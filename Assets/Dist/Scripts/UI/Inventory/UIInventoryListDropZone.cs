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
        if (_window == null || !InventoryDragState.TryGetActive(out InventoryDragPayload payload))
            return;

        InventorySession session = _window.Session;
        InventoryContainer target = _window.SelectedContainer;
        if (session == null || target == null || payload.SourceContainer == null || payload.Stacks == null)
            return;

        if (payload.Stacks.Count == 0)
            return;

        if (payload.SourceContainer == target)
            return;

        if (!session.MoveStacks(payload.SourceContainer, target, payload.Stacks))
            return;

        payload.SourceSelection?.Clear();
    }
}
