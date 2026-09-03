using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// topology Floor 기준 수직 낙하·착지. Physics 바닥 없이 <see cref="MapLogicalFloorProbe"/>만 사용합니다.
    /// 낙하 시 예측 footprint 볼륨이 벽 셀에 진입하면 차단하고 <see cref="MapTopologyDepenetration"/>에 진입 방향을 기록합니다.
    /// </summary>
    public sealed class MapLogicalFloorSupport
    {
        readonly MapLogicalFloorProbe _probe;
        readonly MapTopologyDepenetration _depenetration;
        readonly IMapTopologyQuery _query;
        readonly float _cellSize;

        public MapLogicalFloorSupport(IMapTopologyQuery query, MapTopologyDepenetration depenetration)
        {
            _query = query;
            _cellSize = query.CellSize > 0f ? query.CellSize : 1f;
            _probe = new MapLogicalFloorProbe(query);
            _depenetration = depenetration;
        }

        /// <summary>
        /// 발 높이 기준 지지·낙하를 적용합니다. <paramref name="feetOffset"/> = transform.y - feetWorldY.
        /// </summary>
        public void ApplyVertical(
            ref Vector3 worldPos,
            ref float verticalVelocity,
            float deltaTime,
            float feetOffset,
            ref MapCollisionGrid.FeetCell feet,
            ref MapTopologyDepenetration.Tracker gridTracker,
            Vector3Int footprint,
            float gravity = -9.81f)
        {
            footprint = MapTopologyCollisionResolver.ClampFootprint(footprint);
            if (deltaTime <= 0f)
                return;

            verticalVelocity += gravity * deltaTime;
            float predictedFeetY = feet.FeetY + verticalVelocity * deltaTime;

            if (TryBlockFallIntoWall(feet, predictedFeetY, footprint, ref verticalVelocity, ref gridTracker))
                return;

            if (_probe.TryFindSnapSurface(
                    feet.X,
                    feet.Z,
                    feet.GridY,
                    feet.FeetY,
                    predictedFeetY,
                    out float landingSurfaceY))
            {
                verticalVelocity = 0f;
                worldPos.y = landingSurfaceY + feetOffset;
                feet = MapCollisionGrid.WithFeetY(feet, landingSurfaceY, _cellSize);
                return;
            }

            worldPos.y = predictedFeetY + feetOffset;
            feet = MapCollisionGrid.WithFeetY(feet, predictedFeetY, _cellSize);
        }

        bool TryBlockFallIntoWall(
            MapCollisionGrid.FeetCell feet,
            float predictedFeetY,
            Vector3Int footprint,
            ref float verticalVelocity,
            ref MapTopologyDepenetration.Tracker gridTracker)
        {
            if (_depenetration == null || verticalVelocity >= 0f)
                return false;

            var currentFeetGrid = MapCollisionGrid.ToGrid(feet);
            var predictedFeet = MapCollisionGrid.WithFeetY(feet, predictedFeetY, _cellSize);
            var predictedFeetGrid = MapCollisionGrid.ToGrid(predictedFeet);

            if (!MapTopologyCollisionResolver.FootprintVolumeBlocks(_query, predictedFeetGrid, footprint))
                return false;

            if (!MapTopologyCollisionResolver.FootprintVolumeBlocks(_query, currentFeetGrid, footprint))
                _depenetration.CaptureGridTransition(ref gridTracker, currentFeetGrid, predictedFeetGrid, footprint);

            verticalVelocity = 0f;
            gridTracker.LastFeetGrid = currentFeetGrid;
            gridTracker.HasLastFeetGrid = true;
            return true;
        }
    }
}
