// ============================================================
// IMapHearingPingDriver — Dist.Map 경계용 청각 핑 드라이버 계약
// ============================================================

namespace IsoTilemap
{
    /// <summary>
    /// Implemented by DistScript drivers (e.g. CharacterHearingPingDriver).
    /// Keeps TileMapManager free of reverse asmdef references.
    /// </summary>
    public interface IMapHearingPingDriver
    {
        void Init(TileMapManager map);
        void Shutdown();
    }
}
