// ============================================================
// SightLineSegmentSampler — 카메라↔플레이어 3D 시선 샘플 (건물 resolver와 동일 step)
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public static class SightLineSegmentSampler
    {
        /// <summary>
        /// <see cref="BuildingPlayerOcclusionResolver"/>와 동일한 카메라↔플레이어 3D 샘플 셀 (x, y, z).
        /// </summary>
        public static void CollectSegmentCells(
            Vector3 cameraWorld,
            Vector3 playerWorld,
            float cellSize,
            HashSet<Vector3Int> output)
        {
            output.Clear();

            cellSize = Mathf.Max(1e-4f, cellSize);
            float span = Vector3.Distance(cameraWorld, playerWorld);
            int steps = Mathf.Max(1, Mathf.CeilToInt(span / (cellSize * 0.5f)));

            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0f : i / (float)steps;
                Vector3 p = Vector3.Lerp(cameraWorld, playerWorld, t);
                output.Add(TileHelper.ConvertWorldToGrid(p, cellSize));
            }
        }

        /// <summary>시선 3D 샘플(건물 resolver와 동일 step) + 각 샘플 Y에서 XZ 밴드 확장.</summary>
        public static void CollectBlendBandCells(
            Vector3 cameraWorld,
            Vector3 playerWorld,
            Vector3Int playerCell,
            in SightLineBlendSettings settings,
            HashSet<Vector3Int> output)
        {
            output.Clear();

            float cellSize = Mathf.Max(1e-4f, settings.CellSize);
            int band = Mathf.Max(0, settings.BandRadiusCells);

            var segmentScratch = new HashSet<Vector3Int>();
            CollectSegmentCells(cameraWorld, playerWorld, cellSize, segmentScratch);

            foreach (Vector3Int sample in segmentScratch)
                ExpandChebyshevBand(sample.x, sample.y, sample.z, band, output);

            ExpandChebyshevBand(playerCell.x, playerCell.y, playerCell.z, band, output);
        }

        static void ExpandChebyshevBand(int centerX, int centerY, int centerZ, int radius, HashSet<Vector3Int> output)
        {
            if (radius <= 0)
            {
                output.Add(new Vector3Int(centerX, centerY, centerZ));
                return;
            }

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) > radius)
                        continue;

                    output.Add(new Vector3Int(centerX + dx, centerY, centerZ + dz));
                }
            }
        }
    }
}
