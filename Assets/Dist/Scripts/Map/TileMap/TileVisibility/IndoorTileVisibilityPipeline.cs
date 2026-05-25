// ============================================================
// IndoorTileVisibilityPipeline — 실내 층 가시성 (위층 Hide + scope + peek)
// ============================================================
namespace IsoTilemap
{
    public sealed class IndoorTileVisibilityPipeline
    {
        static readonly TileVisibilityPipeline Pipeline = new(
            new SameBuildingUpperFloorHideLayer(),
            new BuildingScopeLayer(),
            new BelowFloorPeekLayer());

        public bool IsVisible(TileData tile, in FloorVisibilityContext ctx) =>
            Pipeline.IsVisible(tile, ctx);
    }
}
