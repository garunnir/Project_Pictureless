// ============================================================
// HarvestContextAction — 수확 가능 식물에서 열매·씨앗 지급
// ============================================================

using UnityEngine;

public sealed class HarvestContextAction : IContextMenuAction
{
    readonly Vector3Int _cell;

    public HarvestContextAction(Vector3Int cell)
    {
        _cell = cell;
    }

    public string GetDisabledReason() =>
        MapPlantService.GetHarvestBlockedReason(_cell);

    public void Execute() =>
        FarmCellTargetFlow.BeginHarvest();
}
