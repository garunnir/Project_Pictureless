// ============================================================
// CollectTrapContextAction — 물 셀 통발 수확
// ============================================================

using IsoTilemap;
using UnityEngine;

public sealed class CollectTrapContextAction : IContextMenuAction
{
    readonly Vector3Int _cell;

    public CollectTrapContextAction(Vector3Int cell) => _cell = cell;

    public string GetDisabledReason() =>
        MapFishService.CanCollectTrapAt(_cell) ? null : FishTrapContextLabels.CollectBlocked;

    public void Execute() => FishCellTargetFlow.BeginCollectTrap(_cell);
}
