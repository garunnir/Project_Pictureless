// ============================================================
// InventoryScrollDragHandler — 인벤 리스트 스크롤 드래그 (키보드/Player 맵 무관)
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;

public sealed class InventoryScrollDragHandler : MonoBehaviour,
    IBeginDragHandler,
    IEndDragHandler,
    IPointerUpHandler
{
    IInventoryScrollDragHost _host;

    public void Bind(IInventoryScrollDragHost host) => _host = host;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_host == null)
            return;

        _host.OnScrollDragStarted();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_host == null)
            return;

        _host.OnScrollDragEnded();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_host == null)
            return;

        _host.OnScrollDragEnded();
    }

    void OnDisable()
    {
        if (_host == null)
            return;

        _host.OnScrollDragEnded();
    }
}
