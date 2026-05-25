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
            int tileBand = tile.identity.GridPos.y;
            if (tileBand > ctx.FloorBand && tile.identity.buildingId == ctx.PlayerBuildingId)
                return TileVisibilityVerdict.Hide;

            return TileVisibilityVerdict.Continue;
        }
    }

    public sealed class BuildingScopeLayer : ITileVisibilityLayer
    {
        public TileVisibilityVerdict Evaluate(TileData tile, in FloorVisibilityContext ctx)
        {
            int tileBand = tile.identity.GridPos.y;
            if (tileBand < ctx.FloorBand)
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
            int tileBand = tile.identity.GridPos.y;
            if (tileBand >= ctx.FloorBand)
                return TileVisibilityVerdict.Continue;

            var type = (TileView.TileType)tile.identity.tileType;
            if (type is TileView.TileType.Wall or TileView.TileType.EdgeWall or TileView.TileType.Obstacle)
                return TileVisibilityVerdict.Show;

            var gridPos = tile.identity.GridPos;
            if (ctx.VisibleBelowCells.Contains((gridPos.x, gridPos.z, tileBand)))
                return TileVisibilityVerdict.Show;

            return TileVisibilityVerdict.Hide;
        }
    }

    public sealed class BlockingBuildingFullHideLayer : ITileVisibilityLayer
    {
        public TileVisibilityVerdict Evaluate(TileData tile, in FloorVisibilityContext ctx)
        {
            int buildingId = tile.identity.buildingId;
            if (buildingId > 0 && ctx.PlayerBlockingBuildingIds.Contains(buildingId))
                return TileVisibilityVerdict.Hide;

            return TileVisibilityVerdict.Continue;
        }
    }

    public sealed class ShowAllLayer : ITileVisibilityLayer
    {
        public TileVisibilityVerdict Evaluate(TileData tile, in FloorVisibilityContext ctx) =>
            TileVisibilityVerdict.Show;
    }
}
