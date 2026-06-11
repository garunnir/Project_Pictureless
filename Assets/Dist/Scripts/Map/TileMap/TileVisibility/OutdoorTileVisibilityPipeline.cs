// ============================================================

// OutdoorTileVisibilityPipeline — 야외: 시선 차단은 IsTileVisible 선행, 나머지 Show

// ============================================================

namespace IsoTilemap

{

    public sealed class OutdoorTileVisibilityPipeline

    {

        static readonly TileVisibilityPipeline Pipeline = new(new ShowAllLayer());



        public bool IsVisible(TileData tile, in FloorVisibilityContext ctx) =>

            Pipeline.IsVisible(tile, ctx);

    }

}


