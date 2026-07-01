// ============================================================
// VisibilityLayers — 실내/야외 층 가시성 레이어 구현
// ============================================================
using System.Collections.Generic;

namespace IsoTilemap
{
    public sealed class SpaceAboveHideLayer : ITileVisibilityLayer
    {
        public TileVisibilityVerdict Evaluate(TileData tile, in FloorVisibilityContext ctx)
        {
            if (SpaceVisibilityUtil.IsEntirelyAbovePlayerSpace(tile.identity, in ctx))
                return TileVisibilityVerdict.Hide;

            return TileVisibilityVerdict.Continue;
        }
    }

    public sealed class SpaceMembershipShowLayer : ITileVisibilityLayer
    {
        public TileVisibilityVerdict Evaluate(TileData tile, in FloorVisibilityContext ctx)
        {
            return SpaceVisibilityUtil.TouchesPlayerSpace(tile.identity, in ctx)
                ? TileVisibilityVerdict.Show
                : TileVisibilityVerdict.Continue;
        }
    }

    public sealed class BelowSpaceLayer : ITileVisibilityLayer
    {
        public TileVisibilityVerdict Evaluate(TileData tile, in FloorVisibilityContext ctx)
        {
            if (!SpaceVisibilityUtil.IsEntirelyBelowPlayerSpace(tile.identity, in ctx))
                return TileVisibilityVerdict.Continue;

            if (TileIdentityUtil.IsWallLike(tile.identity))
                return TileVisibilityVerdict.Show;

            SpaceVisibilityUtil.TryGetStructuralBand(tile.identity, out _, out int maxY);
            if (TileVisibilityCellUtil.IsSliceInPeekBelow(tile.identity, maxY, in ctx))
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
