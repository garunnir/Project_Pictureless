// ============================================================
// IndoorTileVisibilityPipeline — 실내 Space 기준 structural visibility
// ============================================================
namespace IsoTilemap
{
    public sealed class IndoorTileVisibilityPipeline
    {
        static readonly TileVisibilityPipeline Pipeline = new(
            new SpaceAboveHideLayer(),
            new SpaceMembershipShowLayer(),
            new BelowSpaceLayer());

        public bool IsVisible(TileData tile, in FloorVisibilityContext ctx) =>
            Pipeline.IsVisible(tile, ctx);
    }
}
