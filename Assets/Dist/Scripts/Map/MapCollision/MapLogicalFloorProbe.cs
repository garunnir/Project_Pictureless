namespace IsoTilemap
{
    /// <summary>
    /// 발이 위치한 (x,z,gridY) Floor 한 칸만 검사합니다.
    /// Floor가 있고 다음 프레임 발이 바닥면 아래로 교차하면 스냅합니다.
    /// </summary>
    public sealed class MapLogicalFloorProbe
    {
        readonly IMapTopologyQuery _query;
        readonly float _cellSize;

        const float CrossTolerance = 0.05f;

        public MapLogicalFloorProbe(IMapTopologyQuery query)
        {
            _query = query;
            _cellSize = query.CellSize > 0f ? query.CellSize : 1f;
        }

        public bool TryFindSnapSurface(
            int x,
            int z,
            int feetGridY,
            float predictedFeetY,
            out float landingSurfaceY)
        {
            landingSurfaceY = 0f;

            if (!_query.CellHasFloor(x, z, feetGridY))
                return false;

            float surfaceY = MapCollisionGrid.GridYToSurfaceY(feetGridY, _cellSize);
            if (predictedFeetY >= surfaceY + CrossTolerance)
                return false;

            landingSurfaceY = surfaceY;
            return true;
        }
    }
}
