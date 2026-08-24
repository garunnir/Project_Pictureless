// ============================================================
// TillContextContributor — PLOWABLE/DIGGABLE 셀 경작 (타일 우클릭)
// ============================================================

using System.Collections.Generic;
using IsoTilemap;
using UnityEngine;

public sealed class TillContextContributor : ITileObjectContextMenuContributor
{
    public void Contribute(TileObjectInteractionTarget target, List<ContextMenuEntry> roots)
    {
        if (target == null || roots == null)
            return;
        if (!TryResolveCell(target, out Vector3Int cell))
            return;

        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null || !host.IsTillable(cell))
            return;

        roots.Add(ContextMenuEntry.Leaf(
            "till-cell",
            HarvestContextLabels.Till,
            new TillContextAction(cell)));
    }

    static bool TryResolveCell(TileObjectInteractionTarget target, out Vector3Int cell)
    {
        cell = default;
        MapPlantInteractable plant = target.GetComponentInParent<MapPlantInteractable>();
        if (plant == null)
            plant = target.GetComponentInChildren<MapPlantInteractable>(true);
        if (plant != null)
        {
            cell = plant.Cell;
            return true;
        }

        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null)
            return false;

        cell = host.ResolveCellFromWorld(target.transform.position);
        return true;
    }
}
