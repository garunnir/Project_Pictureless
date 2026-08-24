// ============================================================
// HarvestContextContributor — 식물 오버레이 수확 액션
// ============================================================

using System.Collections.Generic;
using IsoTilemap;

public sealed class HarvestContextContributor : ITileObjectContextMenuContributor
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
        if (host == null || !host.TryGetPlant(plant.Cell, out _))
            return;

        roots.Add(ContextMenuEntry.Leaf(
            "harvest-plant",
            HarvestContextLabels.Harvest,
            new HarvestContextAction(plant.Cell)));
    }
}
