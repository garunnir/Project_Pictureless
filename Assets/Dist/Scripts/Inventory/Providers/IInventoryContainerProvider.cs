// ============================================================
// IInventoryContainerProvider — 월드/플레이어 인벤 컨테이너 제공자
// ============================================================

using UnityEngine;

public interface IInventoryContainerProvider
{
    string ContainerId { get; }
    InventoryContainer Container { get; }
    Vector3 WorldPosition { get; }
    Vector3Int GridPosition { get; }
    bool IsAvailableToPlayer(GameObject player);
}
