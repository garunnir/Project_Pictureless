// ============================================================
// ChopContextContributor — 나무 작물 OccupiedCell 벌목 액션
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using UnityEngine;

public sealed class ChopContextContributor : ITileObjectContextMenuContributor
{
    public void Contribute(TileObjectInteractionTarget target, List<ContextMenuEntry> roots)
    {
        if (target == null || roots == null)
            return;

        if (!TryResolvePlantCell(target, out Vector3Int cell))
            return;

        MapPlantService.CatchUpCell(cell);

        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null || !host.TryGetPlant(cell, out PlantCell plant))
            return;

        ItemData item = GameplayData.GetItem(plant.SeedItemId);
        if (item?.seed == null || !item.seed.IsTree)
            return;

        roots.Add(ContextMenuEntry.Leaf(
            "chop-plant",
            HarvestContextLabels.Chop,
            new ChopContextAction(cell)));
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
