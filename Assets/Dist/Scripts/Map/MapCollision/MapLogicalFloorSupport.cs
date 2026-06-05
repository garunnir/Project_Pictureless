using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// (x,z,band) 점유 Floor 기준 수직 지지·낙하·착지.
    /// Physics 바닥 없이 topology만 사용합니다.
    /// </summary>
    public sealed class MapLogicalFloorSupport
    {
        readonly IMapTopologyQuery _query;
        readonly float _cellSize;
        readonly int[] _bandsDescending;

        const float LandTolerance = 0.05f;

        public MapLogicalFloorSupport(IMapTopologyQuery query, IReadOnlyList<int> distinctBands)
        {
            _query = query;
            _cellSize = query.CellSize > 0f ? query.CellSize : 1f;
            _bandsDescending = CopyBandsDescending(distinctBands);
        }

        public MapLogicalFloorSupport(IMapTopologyQuery query, FloorBandResolver bandResolver)
            : this(query, bandResolver?.DistinctBands)
        {
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

            float feetY = worldPos.y - feetOffset;
            Vector3Int cell = TileHelper.ConvertWorldToGrid(worldPos, _cellSize);
            int x = cell.x;
            int z = cell.z;

            verticalVelocity += gravity * deltaTime;
            float predictedFeetY = feetY + verticalVelocity * deltaTime;

            if (TryFindLandingY(x, z, predictedFeetY, out float landingY))
            {
                verticalVelocity = 0f;
                feetY = landingY;
            }
            else
            {
                feetY = predictedFeetY;
            }

            worldPos.y = feetY + feetOffset;
        }

        bool TryFindLandingY(int x, int z, float predictedFeetY, out float landingY)
        {
            for (int i = 0; i < _bandsDescending.Length; i++)
            {
                int band = _bandsDescending[i];
                if (!_query.CellHasFloor(x, z, band))
                    continue;

                float surfaceY = band * _cellSize;
                if (predictedFeetY <= surfaceY + LandTolerance)
                {
                    landingY = surfaceY;
                    return true;
                }
            }

            landingY = 0f;
            return false;
        }

        static int[] CopyBandsDescending(IReadOnlyList<int> distinctBands)
        {
            if (distinctBands == null || distinctBands.Count == 0)
                return new[] { 0 };

            var copy = new int[distinctBands.Count];
            for (int i = 0; i < distinctBands.Count; i++)
                copy[i] = distinctBands[i];

            Array.Sort(copy);
            Array.Reverse(copy);
            return copy;
        }
    }
}
