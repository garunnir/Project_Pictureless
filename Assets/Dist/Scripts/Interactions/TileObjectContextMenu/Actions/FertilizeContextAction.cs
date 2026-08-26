// ============================================================
// FertilizeContextAction — 식물 1회 비료 (아이템 소비)
// ============================================================

using UnityEngine;

public sealed class FertilizeContextAction : IContextMenuAction
{
    readonly Vector3Int _cell;

    public FertilizeContextAction(Vector3Int cell)
    {
        _cell = cell;
    }

    public string GetDisabledReason() =>
        MapPlantService.GetFertilizeBlockedReason(_cell);

    public void Execute() =>
        FarmCellTargetFlow.BeginFertilizeTile();
}
