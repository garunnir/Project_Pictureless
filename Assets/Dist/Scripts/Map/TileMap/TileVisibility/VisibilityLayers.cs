// ============================================================
// VisibilityLayers — 실내/야외 층 가시성 레이어 구현
// ============================================================
using System.Collections.Generic;

namespace IsoTilemap
{
    public sealed class SameBuildingUpperFloorHideLayer : ITileVisibilityLayer
    {
        public TileVisibilityVerdict Evaluate(TileData tile, in FloorVisibilityContext ctx)
        {
            int sliceY = TileVisibilityCellUtil.GetVisibilitySliceY(tile.identity);
            if (sliceY > ctx.PlayerFloorCellY && tile.identity.buildingId == ctx.PlayerBuildingId)
                return TileVisibilityVerdict.Hide;

            return TileVisibilityVerdict.Continue;
        }
    }

    public sealed class BuildingScopeLayer : ITileVisibilityLayer
    {
        public TileVisibilityVerdict Evaluate(TileData tile, in FloorVisibilityContext ctx)
        {
            int sliceY = TileVisibilityCellUtil.GetVisibilitySliceY(tile.identity);
            if (sliceY < ctx.PlayerFloorCellY)
                return TileVisibilityVerdict.Continue;

            return tile.identity.buildingId == ctx.PlayerBuildingId
                ? TileVisibilityVerdict.Show
                : TileVisibilityVerdict.Hide;
        }
    }

    public sealed class BelowFloorPeekLayer : ITileVisibilityLayer
    {
        public TileVisibilityVerdict Evaluate(TileData tile, in FloorVisibilityContext ctx)
        {
            int sliceY = TileVisibilityCellUtil.GetVisibilitySliceY(tile.identity);
            if (sliceY >= ctx.PlayerFloorCellY)
                return TileVisibilityVerdict.Continue;

            if (TileVisibilityCellUtil.IsSliceInPeekBelow(tile.identity, sliceY, in ctx))
                return TileVisibilityVerdict.Show;

            return TileVisibilityVerdict.Hide;
        }
    }

    /// <summary>야외 시선 차단 building — presentation <c>SightLineBuildingHidden</c>로 반영.</summary>
    public sealed class BlockingBuildingFullHideLayer : ITileVisibilityLayer
    {
        readonly BuildingGroupRegistry _registry;

        public BlockingBuildingFullHideLayer(BuildingGroupRegistry registry) =>
            _registry = registry;

        public TileVisibilityVerdict Evaluate(TileData tile, in FloorVisibilityContext ctx)
        {
            if (!ctx.IsPlayerOutdoor)
                return TileVisibilityVerdict.Continue;

            int buildingId = tile.identity.buildingId;
            if (buildingId <= 0 || !ctx.PlayerBlockingBuildingIds.Contains(buildingId))
                return TileVisibilityVerdict.Continue;

            if (_registry != null &&
                TileIdentityUtil.IsFloorTile(tile.identity) &&
                _registry.IsBottomFloorTile(buildingId, tile.tileDefId))
                return TileVisibilityVerdict.Continue;

            return TileVisibilityVerdict.Hide;
        }
    }

    public sealed class ShowAllLayer : ITileVisibilityLayer
    {
        public TileVisibilityVerdict Evaluate(TileData tile, in FloorVisibilityContext ctx) =>
            TileVisibilityVerdict.Show;
    }
}
