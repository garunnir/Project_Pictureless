// ============================================================
// TileVisibilityPipeline — 레이어 순회, 첫 Show/Hide에서 종료
// ============================================================
using System.Collections.Generic;

namespace IsoTilemap
{
    public interface ITileVisibilityLayer
    {
        TileVisibilityVerdict Evaluate(TileData tile, in FloorVisibilityContext ctx);
    }

    public sealed class TileVisibilityPipeline
    {
        readonly ITileVisibilityLayer[] _layers;

        public TileVisibilityPipeline(params ITileVisibilityLayer[] layers) => _layers = layers;

        public bool IsVisible(TileData tile, in FloorVisibilityContext ctx)
        {
            for (int i = 0; i < _layers.Length; i++)
            {
                switch (_layers[i].Evaluate(tile, ctx))
                {
                    case TileVisibilityVerdict.Show:
                        return true;
                    case TileVisibilityVerdict.Hide:
                        return false;
                }
            }

            return false;
        }
    }
}
