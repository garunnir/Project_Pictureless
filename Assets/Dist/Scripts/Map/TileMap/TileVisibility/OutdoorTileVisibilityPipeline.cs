// ============================================================
// OutdoorTileVisibilityPipeline — 야외: 가림 건물 통째 Hide, 나머지 Show
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
