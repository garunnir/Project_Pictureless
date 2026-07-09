// ============================================================
// IInventoryItemDragHost — 아이템 드래그 overlay·입력 억제
// ============================================================

public interface IInventoryItemDragHost
{
    void OnItemDragStarted();
    void OnItemDragEnded();
    void UpdateDragGhost(UnityEngine.Vector2 screenPosition, int stackCount);
    void HideDragGhost();
}
