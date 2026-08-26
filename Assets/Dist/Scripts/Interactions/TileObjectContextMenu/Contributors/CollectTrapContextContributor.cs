// ============================================================
// CollectTrapContextContributor — 물 셀 통발 수확 메뉴
// ============================================================

using System.Collections.Generic;
using IsoTilemap;
using UnityEngine;

public sealed class CollectTrapContextContributor : ITileObjectContextMenuContributor
{
    public void Contribute(TileObjectInteractionTarget target, List<ContextMenuEntry> roots)
    {
        if (target == null || roots == null)
            return;

        if (!TryResolveTrapCell(target, out Vector3Int cell))
            return;

        MapFishTrapHost host = MapFishTrapHost.Runtime ?? MapFishTrapHost.EnsureRuntime();
        host?.CatchUpCell(cell);
        if (host == null || !host.HasTrap(cell))
            return;

        roots.Add(ContextMenuEntry.Leaf(
            "collect-fish-trap",
            FishTrapContextLabels.Collect,
            new CollectTrapContextAction(cell)));
    }

    static bool TryResolveTrapCell(TileObjectInteractionTarget target, out Vector3Int cell)
    {
        cell = default;
        MapFishTrapInteractable trap = target.GetComponentInParent<MapFishTrapInteractable>();
        if (trap == null)
            trap = target.GetComponentInChildren<MapFishTrapInteractable>(true);
        if (trap != null)
        {
            cell = trap.Cell;
            return true;
        }

        return false;
    }
}
