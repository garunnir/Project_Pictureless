// ============================================================
// IInventoryItemDragHost — 아이템 드래그 overlay·입력 억제
// ============================================================

public interface IInventoryItemDragHost
{
    void OnItemDragStarted();
    void OnItemDragEnded();
    void BeginDragGhost(UnityEngine.Vector2 screenPosition, int stackCount);
    void UpdateDragGhostPosition(UnityEngine.Vector2 screenPosition);
    void HideDragGhost();
}
