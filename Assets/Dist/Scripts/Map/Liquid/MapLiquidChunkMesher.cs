// ============================================================
// MapLiquidChunkMesher — 액체 청크 하나를 수면 메시로 굽는다 (코너 평균 높이 · 노출면만)
// ============================================================
// 이음매가 없는 이유:
// - 수면 높이는 셀이 아니라 **격자 코너**에서 결정된다. 코너 값은 그 코너를 공유하는 4개 셀의
//   평균이며, 이웃 셀을 오버레이에서 직접 조회하므로 청크 경계 밖 셀도 같은 값을 만든다.
//   → 인접 셀·인접 청크가 같은 코너 좌표에서 정확히 같은 높이를 낸다(틈·계단 없음).
// - 물에 잠긴 셀(위 칸에도 물)은 Fill 1로 취급해 기둥이 세로로도 연속된다.
// - 측면은 마른 이웃 쪽에만 만든다 — 물끼리 맞닿은 내부 면은 생성하지 않아 알파 겹침이 없다.
//
// 윗면 법선은 경사와 무관하게 up으로 고정한다. 파도 음영은 셰이더의 월드 XZ 노이즈가 만들고,
// 화면은 DistPixelisationFeature가 픽셀화하므로 코너 경사에서 나오는 미세 음영은 어차피 뭉갠다.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace IsoTilemap
{
    public sealed class MapLiquidChunkMesher
    {
        /// <summary>격자 코너 하나를 공유하는 셀 수 (2×2).</summary>
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

        /// <summary>청크 하나를 <paramref name="mesh"/>에 굽는다. 정점은 청크 원점 기준 로컬 좌표.</summary>
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

        /// <summary>
        /// 한 층을 굽고 그 층에 **액체가 있었는지**를 돌려준다. 삼각형 유무가 아니라 액체 유무인 것이 중요하다 —
        /// 사방·위가 모두 물인 내부 층은 노출면이 하나도 없어 삼각형이 0개지만, 스캔 범위에서 빠지면
        /// 위층 물이 빠졌을 때 이 층의 수면을 만들 기회를 영영 잃는다.
        /// 물이 없으면 코너 계산을 건너뛴다(빈 층 비용 = 셀 수만큼의 조회).
        /// </summary>
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

            int i00 = localX * _cornerStride + localZ;
            int i10 = (localX + 1) * _cornerStride + localZ;
            int i01 = localX * _cornerStride + localZ + 1;
            int i11 = (localX + 1) * _cornerStride + localZ + 1;

            float x0 = x * _cellSize - chunkOrigin.x;
            float x1 = (x + 1) * _cellSize - chunkOrigin.x;
            float z0 = z * _cellSize - chunkOrigin.z;
            float z1 = (z + 1) * _cellSize - chunkOrigin.z;

            float h00 = bottomY + SurfaceLift(_corners[i00].HeightFill);
            float h10 = bottomY + SurfaceLift(_corners[i10].HeightFill);
            float h01 = bottomY + SurfaceLift(_corners[i01].HeightFill);
            float h11 = bottomY + SurfaceLift(_corners[i11].HeightFill);

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
                if (EffectiveFill01(x + dir.x, y, z + dir.y) > MapLiquidRenderConsts.MinVisibleFill01)
                    continue;

                ResolveSideEdge(
                    dir,
                    x0, x1, z0, z1,
                    h00, h10, h01, h11,
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

        /// <summary>같은 층의 윗면끼리 코너 정점을 공유한다 — 코너 값이 격자 기준이라 색·법선이 동일하다.</summary>
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
            // 측면은 물가라서 폼을 세로 밴드로 깔아 준다 — 위/아래 정점 모두 코너 폼 값을 쓴다.
            int ba = AddVertex(bottomA, normal, cornerA, isTop: false);
            int ta = AddVertex(topA, normal, cornerA, isTop: false);
            int bb = AddVertex(bottomB, normal, cornerB, isTop: false);
            int tb = AddVertex(topB, normal, cornerB, isTop: false);

            // SurfaceLift가 항상 양수라 topA != bottomA가 보장되고, 그래서 이 외적은 0벡터가 되지 않는다.
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

        float SurfaceLift(float heightFill) =>
            Mathf.Max(heightFill, MapLiquidRenderConsts.SurfaceMinLift01) * _cellSize;

        /// <summary>
        /// 격자 코너 (lx, lz)를 공유하는 4개 셀의 집계. 청크 경계와 무관하게 오버레이를 직접 읽으므로
        /// 어느 셀·어느 청크에서 물어도 같은 값이 나온다 — 이것이 이음매를 없앤다.
        /// 넷 중 하나라도 잠긴 셀이면 코너를 셀 천장(1.0)으로 올린다. 그래야 위 층 기둥의 측면이
        /// 시작하는 높이와 아래 층 수면이 정확히 맞물려 층 사이에 구멍이 생기지 않는다.
        /// </summary>
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

            // 높이는 마른 이웃을 0으로 쳐서 물가가 내려앉게, 색은 젖은 이웃만 평균해서 얕아 보이지 않게.
            return new CornerData(
                anySubmerged ? 1f : wetSum * CornerCellWeight,
                wetCount > 0 ? wetSum / wetCount : 0f,
                (CornerCellCount - wetCount) * CornerCellWeight);
        }

        /// <summary>위 칸에 물이 있으면 잠긴 셀이므로 가득 찬 것으로 본다 — 세로로 이어진 기둥.</summary>
        float EffectiveFill01(int x, int y, int z)
        {
            float fill = CellFill01(x, y, z);
            if (fill <= MapLiquidRenderConsts.MinVisibleFill01)
                return 0f;

            return CellFill01(x, y + 1, z) > MapLiquidRenderConsts.MinVisibleFill01 ? 1f : fill;
        }

        float CellFill01(int x, int y, int z) =>
            MapLiquidConsts.ToFill01(_overlay.GetEffectiveMl(new Vector3Int(x, y, z)));

        static byte To255(float value01) => (byte)Mathf.RoundToInt(Mathf.Clamp01(value01) * 255f);

        public readonly struct BuildResult
        {
            public static readonly BuildResult Empty = new(false, false, 0, 0);

            /// <summary>스캔 범위 안에 액체가 있었는가. 노출면이 없어 삼각형이 0개인 경우와 구분된다.</summary>
            public readonly bool HasLiquid;

            public readonly bool HasGeometry;

            /// <summary>액체가 실제로 존재한 층 범위 — 호출부가 다음 리메시 스캔 범위를 좁히는 데 쓴다.</summary>
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
            /// <summary>마른 이웃을 0으로 친 평균 — 물가에서 수면이 자연스럽게 내려앉는다.</summary>
            public readonly float HeightFill;

            /// <summary>젖은 이웃만의 평균 — 물가 경사 때문에 색이 얕아 보이는 것을 막는다.</summary>
            public readonly float DepthFill;

            /// <summary>마른 이웃 비율(0..1) — 셰이더 폼 밴드 입력.</summary>
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
