// ============================================================
// ChopContextAction — 나무 작물 벌목 (성장 단계별 드롭)
// ============================================================

using UnityEngine;

public sealed class ChopContextAction : IContextMenuAction
{
    readonly Vector3Int _cell;

    public ChopContextAction(Vector3Int cell)
    {
        _cell = cell;
    }

    public string GetDisabledReason() =>
        MapPlantService.GetChopBlockedReason(_cell);

    public void Execute() =>
        FarmCellTargetFlow.BeginChop();
}
