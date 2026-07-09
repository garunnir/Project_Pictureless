// ============================================================
// UIContainerSlotDropZone — 사이드바 탭 드롭·호버 힌트
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UIContainerSlotDropZone : MonoBehaviour,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    UIInventoryListWindow _window;
    UIContainerSlot _slot;

    public void Bind(UIInventoryListWindow window, UIContainerSlot slot)
    {
        _window = window;
        _slot = slot;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_window == null || _slot == null || !InventoryDragState.TryGetActive(out InventoryDragPayload payload))
            return;

        InventorySession session = _window.Session;
        InventoryContainer target = _slot.Container;
        if (session == null || target == null || payload.SourceContainer == null || payload.Stacks == null)
            return;

        if (payload.SourceContainer == target)
            return;

        if (!session.MoveStacks(payload.SourceContainer, target, payload.Stacks))
            return;

        if (payload.Kind == InventoryDragKind.Item)
            payload.SourceSelection?.Clear();

        _slot.SetDropHover(false);
        _window.SelectContainer(target);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!InventoryDragState.IsDragging || _slot == null)
            return;

        _slot.SetDropHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_slot != null)
            _slot.SetDropHover(false);
    }
}
