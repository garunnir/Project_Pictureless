// ============================================================
// IMapSightFadeDriver — Dist.Map 경계용 시야 페이드 드라이버 계약
// ============================================================

namespace IsoTilemap
{
    /// <summary>
    /// Implemented by DistScript drivers (e.g. CharacterSightFadeDriver).
    /// Keeps TileMapManager free of reverse asmdef references.
    /// </summary>
    public interface IMapSightFadeDriver
    {
        void Init(TileMapManager map);
        void Shutdown();
    }
}
