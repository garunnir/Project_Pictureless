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
            int tileCellY = TileVisibilityCellUtil.GetCellY(tile);
            if (tileCellY > ctx.PlayerFloorCellY && tile.identity.buildingId == ctx.PlayerBuildingId)
                return TileVisibilityVerdict.Hide;

            return TileVisibilityVerdict.Continue;
        }
    }

    public sealed class BuildingScopeLayer : ITileVisibilityLayer
    {
        public TileVisibilityVerdict Evaluate(TileData tile, in FloorVisibilityContext ctx)
        {
            int tileCellY = TileVisibilityCellUtil.GetCellY(tile);
            if (tileCellY < ctx.PlayerFloorCellY)
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
            int tileCellY = TileVisibilityCellUtil.GetCellY(tile);
            if (tileCellY >= ctx.PlayerFloorCellY)
                return TileVisibilityVerdict.Continue;

            var type = (TileView.TileType)tile.identity.tileType;
            if (type is TileView.TileType.Wall or TileView.TileType.EdgeWall)
                return TileVisibilityVerdict.Show;

            var gridPos = tile.identity.GridPos;
            if (type == TileView.TileType.Floor)
                gridPos = FloorFaceKey.FromFloorTileIdentity(tile.identity).CellAbove;

            if (ctx.VisibleBelowCells.Contains((gridPos.x, gridPos.z, tileCellY)))
                return TileVisibilityVerdict.Show;

            return TileVisibilityVerdict.Hide;
        }
    }

    public sealed class BlockingBuildingFullHideLayer : ITileVisibilityLayer
    {
        public TileVisibilityVerdict Evaluate(TileData tile, in FloorVisibilityContext ctx)
        {
            if (!ctx.IsPlayerOutdoor)
                return TileVisibilityVerdict.Continue;

            int buildingId = tile.identity.buildingId;
            if (buildingId <= 0 || !ctx.PlayerBlockingBuildingIds.Contains(buildingId))
                return TileVisibilityVerdict.Continue;

            if ((TileView.TileType)tile.identity.tileType == TileView.TileType.Floor &&
                FloorFaceKey.FromFloorTileIdentity(tile.identity).CellAbove.y == ctx.MinCellY)
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
