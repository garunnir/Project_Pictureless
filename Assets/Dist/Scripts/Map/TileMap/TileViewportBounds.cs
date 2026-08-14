using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>월드 AABB → 청크 집합. 지면 footprint 수학은 CameraGroundView.</summary>
    public static class TileViewportBounds
    {
        public static void AppendChunksForWorldBounds(
            HashSet<Vector2Int> chunks,
            float minX,
            float maxX,
            float minZ,
            float maxZ,
            float cellSize,
            int chunkSize,
            int marginChunks)
        {
            if (chunks == null)
                return;

            cellSize = Mathf.Max(1e-4f, cellSize);
            chunkSize = Mathf.Max(1, chunkSize);
            marginChunks = Mathf.Max(0, marginChunks);

            Vector3Int minCell = TileHelper.ConvertWorldToGrid(new Vector3(minX, 0f, minZ), cellSize);
            Vector3Int maxCell = TileHelper.ConvertWorldToGrid(new Vector3(maxX, 0f, maxZ), cellSize);

            int minCx = TileChunkCoord.FromCell(minCell, chunkSize).x - marginChunks;
            int maxCx = TileChunkCoord.FromCell(maxCell, chunkSize).x + marginChunks;
            int minCz = TileChunkCoord.FromCell(minCell, chunkSize).y - marginChunks;
            int maxCz = TileChunkCoord.FromCell(maxCell, chunkSize).y + marginChunks;

            if (minCx > maxCx)
                (minCx, maxCx) = (maxCx, minCx);
            if (minCz > maxCz)
                (minCz, maxCz) = (maxCz, minCz);

            for (int cx = minCx; cx <= maxCx; cx++)
            {
                for (int cz = minCz; cz <= maxCz; cz++)
                    chunks.Add(new Vector2Int(cx, cz));
            }
        }

        /// <summary>풀 피크 추정용 — CameraGroundView.OrthoAxisSpan 기준 청크 반경.</summary>
        public static int ComputeCameraChunkRadius(
            float orthographicSize,
            float aspect,
            float cellSize,
            int chunkSize,
            int marginChunks)
        {
            float chunkWorld = Mathf.Max(1, chunkSize) * Mathf.Max(1e-4f, cellSize);
            float axisSpan = CameraGroundView.OrthoAxisSpan(orthographicSize, aspect);
            int orthoRadius = Mathf.CeilToInt(axisSpan / chunkWorld);
            return Mathf.Max(0, marginChunks) + orthoRadius;
        }
    }
}
