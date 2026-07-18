// ============================================================
// InventorySidebarScrollRect — 사이드바 스크롤 (탭 DnD 중에는 스크롤 드래그 무시)
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class InventorySidebarScrollRect : ScrollRect
{
    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (ShouldIgnoreScrollDrag(eventData))
            return;

        base.OnBeginDrag(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (ShouldIgnoreScrollDrag(eventData))
            return;

        base.OnDrag(eventData);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (InventoryDragState.IsDragging)
            return;

        base.OnEndDrag(eventData);
    }

    static bool ShouldIgnoreScrollDrag(PointerEventData eventData)
    {
        if (InventoryDragState.IsDragging)
            return true;

        GameObject press = eventData.pointerPressRaycast.gameObject;
        if (press == null)
            press = eventData.pointerEnter;

        return press != null && press.GetComponentInParent<UIContainerSlot>() != null;
    }
}
