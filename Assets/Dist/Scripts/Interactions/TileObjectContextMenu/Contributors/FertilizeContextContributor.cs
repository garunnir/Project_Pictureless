// ============================================================
// FertilizeContextContributor — 식물 오버레이 비료 (타일 우클릭)
// ============================================================

using System.Collections.Generic;
using IsoTilemap;

public sealed class FertilizeContextContributor : ITileObjectContextMenuContributor
{
    public void Contribute(TileObjectInteractionTarget target, List<ContextMenuEntry> roots)
    {
        if (target == null || roots == null)
            return;

        MapPlantInteractable plant = target.GetComponentInParent<MapPlantInteractable>();
        if (plant == null)
            plant = target.GetComponentInChildren<MapPlantInteractable>(true);
        if (plant == null)
            return;

        MapPlantService.CatchUpCell(plant.Cell);

        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null || !host.TryGetPlant(plant.Cell, out PlantCell cell) || cell.Fertilized)
            return;

        roots.Add(ContextMenuEntry.Leaf(
            "fertilize-plant",
            HarvestContextLabels.Fertilize,
            new FertilizeContextAction(plant.Cell)));
    }
}
