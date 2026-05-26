// ============================================================
// OutdoorTileVisibilityPipeline — 야외: 가림 건물은 1층 바닥 제외 Hide, 나머지 Show
// ============================================================
namespace IsoTilemap
{
    public sealed class OutdoorTileVisibilityPipeline
    {
        static readonly TileVisibilityPipeline Pipeline = new(
            new BlockingBuildingFullHideLayer(),
            new ShowAllLayer());

        public bool IsVisible(TileData tile, in FloorVisibilityContext ctx) =>
            Pipeline.IsVisible(tile, ctx);
    }
}
