// ============================================================
// FertilizeContextContributor — 식물 OccupiedCell 비료 (타일 우클릭)
// ============================================================

using System.Collections.Generic;
using IsoTilemap;
using UnityEngine;

public sealed class FertilizeContextContributor : ITileObjectContextMenuContributor
{
    public void Contribute(TileObjectInteractionTarget target, List<ContextMenuEntry> roots)
    {
        if (target == null || roots == null)
            return;

        if (!TryResolvePlantCell(target, out Vector3Int cell))
            return;

        MapPlantService.CatchUpCell(cell);

        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null || !host.TryGetPlant(cell, out PlantCell plant) || plant.Fertilized)
            return;

        roots.Add(ContextMenuEntry.Leaf(
            "fertilize-plant",
            HarvestContextLabels.Fertilize,
            new FertilizeContextAction(cell)));
    }

    static bool TryResolvePlantCell(TileObjectInteractionTarget target, out Vector3Int cell)
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

        PlantTileInteractable plantTile = target.GetComponentInParent<PlantTileInteractable>();
        if (plantTile == null)
            plantTile = target.GetComponentInChildren<PlantTileInteractable>(true);
        if (plantTile == null)
            return false;

        cell = plantTile.Cell;
        return true;
    }
}
