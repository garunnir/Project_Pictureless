// ============================================================
// MapLiquidChunkMesher — 액체 청크 수면 메시 (코너 평균 높이 · 노출면만)
// ============================================================
// - 수면 높이는 격자 코너에서 4셀 평균. 오버레이 직접 조회 → 청크 경계 이음매 없음.
// - 잠긴 셀(위 칸에도 물) 측면만 천장까지(SideSurfaceLift) — 층 사이 슬릿 방지.
// - 측면은 마른/비연결 이웃 쪽만. equalize 잔량은 SideWallConnectMinRatio01로 연결 판정.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace IsoTilemap
{
    public sealed class MapLiquidChunkMesher
    {
        const int CornerCellCount = 4;
        const float CornerCellWeight = 1f / CornerCellCount;

        static readonly Vector2Int[] SideDirs =
        {
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1),
        };

        readonly List<Vector3> _positions = new();
        readonly List<Vector3> _normals = new();
        readonly List<Color32> _colors = new();
        readonly List<int> _indices = new();

        CornerData[] _corners;
        int[] _topVertices;
        int _cornerStride;

        MapLiquidOverlay _overlay;
        float _cellSize = 1f;

        public void Bind(MapLiquidOverlay overlay, float cellSize)
        {
            _overlay = overlay;
            _cellSize = Mathf.Max(1e-4f, cellSize);
        }

        public BuildResult Build(
            Mesh mesh,
            Vector2Int chunk,
            int chunkSize,
            int minCellY,
            int maxCellY,
            Vector3 chunkOrigin)
        {
            _positions.Clear();
            _normals.Clear();
            _colors.Clear();
            _indices.Clear();

            if (_overlay == null || mesh == null)
                return BuildResult.Empty;

            chunkSize = Mathf.Max(1, chunkSize);
            EnsureCornerBuffers(chunkSize);

            int minX = chunk.x * chunkSize;
            int minZ = chunk.y * chunkSize;
            int liquidMinY = minCellY;
            int liquidMaxY = minCellY;
            bool anyLiquid = false;

            for (int y = minCellY; y <= maxCellY; y++)
            {
                if (!AppendLayer(minX, minZ, chunkSize, y, chunkOrigin))
                    continue;

                if (!anyLiquid)
                {
                    liquidMinY = y;
                    anyLiquid = true;
                }

                liquidMaxY = y;
            }

            mesh.Clear();
            if (_indices.Count == 0)
                return new BuildResult(anyLiquid, false, liquidMinY, liquidMaxY);

            mesh.indexFormat = _positions.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.subMeshCount = 1;
            mesh.SetVertices(_positions);
            mesh.SetNormals(_normals);
            mesh.SetColors(_colors);
            mesh.SetTriangles(_indices, 0, true);
            return new BuildResult(true, true, liquidMinY, liquidMaxY);
        }

        void EnsureCornerBuffers(int chunkSize)
        {
            int stride = chunkSize + 1;
            if (_cornerStride == stride && _corners != null)
                return;

            _cornerStride = stride;
            _corners = new CornerData[stride * stride];
            _topVertices = new int[stride * stride];
        }

        bool AppendLayer(int minX, int minZ, int chunkSize, int y, Vector3 chunkOrigin)
        {
            if (!LayerHasLiquid(minX, minZ, chunkSize, y))
                return false;

            for (int lx = 0; lx < _cornerStride; lx++)
            {
                for (int lz = 0; lz < _cornerStride; lz++)
                {
                    int index = lx * _cornerStride + lz;
                    _corners[index] = SampleCorner(minX + lx, y, minZ + lz);
                    _topVertices[index] = -1;
                }
            }

            for (int dx = 0; dx < chunkSize; dx++)
            {
                for (int dz = 0; dz < chunkSize; dz++)
                    AppendCell(minX + dx, y, minZ + dz, dx, dz, chunkOrigin);
            }

            return true;
        }

        bool LayerHasLiquid(int minX, int minZ, int chunkSize, int y)
        {
            for (int dx = 0; dx < chunkSize; dx++)
            {
                for (int dz = 0; dz < chunkSize; dz++)
                {
                    if (CellFill01(minX + dx, y, minZ + dz) > MapLiquidRenderConsts.MinVisibleFill01)
                        return true;
                }
            }

            return false;
        }

        void AppendCell(int x, int y, int z, int localX, int localZ, Vector3 chunkOrigin)
        {
            if (CellFill01(x, y, z) <= MapLiquidRenderConsts.MinVisibleFill01)
                return;

            bool submerged = CellFill01(x, y + 1, z) > MapLiquidRenderConsts.MinVisibleFill01;
            float bottomY = y * _cellSize - chunkOrigin.y;
            float selfEffectiveFill = EffectiveFill01(x, y, z);

            int i00 = localX * _cornerStride + localZ;
            int i10 = (localX + 1) * _cornerStride + localZ;
            int i01 = localX * _cornerStride + localZ + 1;
            int i11 = (localX + 1) * _cornerStride + localZ + 1;

            float x0 = x * _cellSize - chunkOrigin.x;
            float x1 = (x + 1) * _cellSize - chunkOrigin.x;
            float z0 = z * _cellSize - chunkOrigin.z;
            float z1 = (z + 1) * _cellSize - chunkOrigin.z;

            float h00 = bottomY + SurfaceLift(_corners[i00]);
            float h10 = bottomY + SurfaceLift(_corners[i10]);
            float h01 = bottomY + SurfaceLift(_corners[i01]);
            float h11 = bottomY + SurfaceLift(_corners[i11]);

            float sideH00 = bottomY + SideSurfaceLift(i00, submerged);
            float sideH10 = bottomY + SideSurfaceLift(i10, submerged);
            float sideH01 = bottomY + SideSurfaceLift(i01, submerged);
            float sideH11 = bottomY + SideSurfaceLift(i11, submerged);

            if (!submerged)
            {
                int a = SharedTopVertex(i00, x0, h00, z0);
                int b = SharedTopVertex(i01, x0, h01, z1);
                int c = SharedTopVertex(i10, x1, h10, z0);
                int d = SharedTopVertex(i11, x1, h11, z1);
                _indices.Add(a);
                _indices.Add(b);
                _indices.Add(c);
                _indices.Add(c);
                _indices.Add(b);
                _indices.Add(d);
            }

            for (int i = 0; i < SideDirs.Length; i++)
            {
                Vector2Int dir = SideDirs[i];
                if (NeighborSuppressesSideWall(x + dir.x, y, z + dir.y, selfEffectiveFill))
                    continue;

                ResolveSideEdge(
                    dir,
                    x0, x1, z0, z1,
                    sideH00, sideH10, sideH01, sideH11,
                    i00, i10, i01, i11,
                    out Vector3 topA,
                    out Vector3 topB,
                    out int cornerA,
                    out int cornerB);

                var bottomA = new Vector3(topA.x, bottomY, topA.z);
                var bottomB = new Vector3(topB.x, bottomY, topB.z);
                var normal = new Vector3(dir.x, 0f, dir.y);
                AddSideQuad(bottomA, topA, cornerA, bottomB, topB, cornerB, normal);
            }
        }

        int SharedTopVertex(int cornerIndex, float x, float y, float z)
        {
            int existing = _topVertices[cornerIndex];
            if (existing >= 0)
                return existing;

            int created = AddVertex(new Vector3(x, y, z), Vector3.up, cornerIndex, isTop: true);
            _topVertices[cornerIndex] = created;
            return created;
        }

        static void ResolveSideEdge(
            Vector2Int dir,
            float x0, float x1, float z0, float z1,
            float h00, float h10, float h01, float h11,
            int i00, int i10, int i01, int i11,
            out Vector3 topA,
            out Vector3 topB,
            out int cornerA,
            out int cornerB)
        {
            if (dir.x > 0)
            {
                topA = new Vector3(x1, h10, z0);
                topB = new Vector3(x1, h11, z1);
                cornerA = i10;
                cornerB = i11;
                return;
            }

            if (dir.x < 0)
            {
                topA = new Vector3(x0, h00, z0);
                topB = new Vector3(x0, h01, z1);
                cornerA = i00;
                cornerB = i01;
                return;
            }

            if (dir.y > 0)
            {
                topA = new Vector3(x0, h01, z1);
                topB = new Vector3(x1, h11, z1);
                cornerA = i01;
                cornerB = i11;
                return;
            }

            topA = new Vector3(x0, h00, z0);
            topB = new Vector3(x1, h10, z0);
            cornerA = i00;
            cornerB = i10;
        }

        void AddSideQuad(
            Vector3 bottomA,
            Vector3 topA,
            int cornerA,
            Vector3 bottomB,
            Vector3 topB,
            int cornerB,
            Vector3 normal)
        {
            int ba = AddVertex(bottomA, normal, cornerA, isTop: false);
            int ta = AddVertex(topA, normal, cornerA, isTop: false);
            int bb = AddVertex(bottomB, normal, cornerB, isTop: false);
            int tb = AddVertex(topB, normal, cornerB, isTop: false);

            bool flip = Vector3.Dot(Vector3.Cross(bottomB - bottomA, topA - bottomA), normal) < 0f;
            if (flip)
            {
                _indices.Add(ba);
                _indices.Add(ta);
                _indices.Add(bb);
                _indices.Add(bb);
                _indices.Add(ta);
                _indices.Add(tb);
                return;
            }

            _indices.Add(ba);
            _indices.Add(bb);
            _indices.Add(ta);
            _indices.Add(ta);
            _indices.Add(bb);
            _indices.Add(tb);
        }

        int AddVertex(Vector3 position, Vector3 normal, int cornerIndex, bool isTop)
        {
            CornerData corner = _corners[cornerIndex];
            _positions.Add(position);
            _normals.Add(normal);
            _colors.Add(new Color32(
                To255(corner.DepthFill),
                To255(corner.Foam),
                isTop ? (byte)255 : (byte)0,
                255));
            return _positions.Count - 1;
        }

        float SurfaceLift(in CornerData corner)
        {
            float lift01 = Mathf.Max(corner.HeightFill, MapLiquidRenderConsts.SurfaceMinLift01);
            float ceiling = 1f - MapLiquidRenderConsts.SurfaceTopInset01;
            if (lift01 > ceiling)
                lift01 = ceiling;

            return lift01 * _cellSize;
        }

        float SideSurfaceLift(int cornerIndex, bool cellSubmerged) =>
            cellSubmerged ? _cellSize : SurfaceLift(_corners[cornerIndex]);

        CornerData SampleCorner(int lx, int y, int lz)
        {
            float wetSum = 0f;
            int wetCount = 0;
            bool anySubmerged = false;

            for (int dx = -1; dx <= 0; dx++)
            {
                for (int dz = -1; dz <= 0; dz++)
                {
                    int cx = lx + dx;
                    int cz = lz + dz;
                    float own = CellFill01(cx, y, cz);
                    if (own <= MapLiquidRenderConsts.MinVisibleFill01)
                        continue;

                    bool submerged = CellFill01(cx, y + 1, cz) > MapLiquidRenderConsts.MinVisibleFill01;
                    if (submerged)
                        anySubmerged = true;

                    wetSum += submerged ? 1f : own;
                    wetCount++;
                }
            }

            return new CornerData(
                anySubmerged ? 1f : wetSum * CornerCellWeight,
                wetCount > 0 ? wetSum / wetCount : 0f,
                (CornerCellCount - wetCount) * CornerCellWeight);
        }

        float EffectiveFill01(int x, int y, int z)
        {
            float fill = CellFill01(x, y, z);
            if (fill <= MapLiquidRenderConsts.MinVisibleFill01)
                return 0f;

            return CellFill01(x, y + 1, z) > MapLiquidRenderConsts.MinVisibleFill01 ? 1f : fill;
        }

        bool NeighborSuppressesSideWall(int nx, int y, int nz, float selfEffectiveFill)
        {
            float neighborFill = EffectiveFill01(nx, y, nz);
            if (neighborFill <= MapLiquidRenderConsts.MinVisibleFill01)
                return false;

            if (neighborFill >= 1f - 1e-4f)
                return true;

            return neighborFill >= selfEffectiveFill * MapLiquidRenderConsts.SideWallConnectMinRatio01;
        }

        float CellFill01(int x, int y, int z) =>
            MapLiquidConsts.ToFill01(_overlay.GetEffectiveMl(new Vector3Int(x, y, z)));

        static byte To255(float value01) => (byte)Mathf.RoundToInt(Mathf.Clamp01(value01) * 255f);

        public readonly struct BuildResult
        {
            public static readonly BuildResult Empty = new(false, false, 0, 0);

            public readonly bool HasLiquid;
            public readonly bool HasGeometry;
            public readonly int MinY;
            public readonly int MaxY;

            public BuildResult(bool hasLiquid, bool hasGeometry, int minY, int maxY)
            {
                HasLiquid = hasLiquid;
                HasGeometry = hasGeometry;
                MinY = minY;
                MaxY = maxY;
            }
        }

        readonly struct CornerData
        {
            public readonly float HeightFill;
            public readonly float DepthFill;
            public readonly float Foam;

            public CornerData(float heightFill, float depthFill, float foam)
            {
                HeightFill = heightFill;
                DepthFill = depthFill;
                Foam = foam;
            }
        }
    }
}
