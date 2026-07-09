// ============================================================
// IInventoryOverlayController — 인벤 오버레이 제어 추상화
// ============================================================

public interface IInventoryOverlayController
{
    void OpenInventory();
    void CloseInventory();
    void ToggleInventory();
    void TogglePrimaryWindow();
    void ToggleLootWindow();
    void OpenLoot(InventoryContainer focusContainer);
}
