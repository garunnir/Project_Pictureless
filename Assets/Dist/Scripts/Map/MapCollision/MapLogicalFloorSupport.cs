using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// topology Floor 기준 수직 낙하·착지. Physics 바닥 없이 <see cref="MapLogicalFloorProbe"/>만 사용합니다.
    /// </summary>
    public sealed class MapLogicalFloorSupport
    {
        readonly MapLogicalFloorProbe _probe;
        readonly float _cellSize;

        public MapLogicalFloorSupport(IMapTopologyQuery query)
        {
            _cellSize = query.CellSize > 0f ? query.CellSize : 1f;
            _probe = new MapLogicalFloorProbe(query);
        }

        /// <summary>
        /// 발 높이 기준 지지·낙하를 적용합니다. <paramref name="feetOffset"/> = transform.y - feetWorldY.
        /// </summary>
        public void ApplyVertical(
            ref Vector3 worldPos,
            ref float verticalVelocity,
            float deltaTime,
            float feetOffset,
            float gravity = -9.81f)
        {
            if (deltaTime <= 0f)
                return;

            var feet = MapCollisionGrid.ResolveFeetCell(worldPos, feetOffset, _cellSize);

            verticalVelocity += gravity * deltaTime;
            float predictedFeetY = feet.FeetY + verticalVelocity * deltaTime;

            if (_probe.TryFindSnapSurface(
                    feet.X,
                    feet.Z,
                    feet.GridY,
                    predictedFeetY,
                    out float landingSurfaceY))
            {
                verticalVelocity = 0f;
                worldPos.y = landingSurfaceY + feetOffset;
                return;
            }

            worldPos.y = predictedFeetY + feetOffset;
        }
    }
}
