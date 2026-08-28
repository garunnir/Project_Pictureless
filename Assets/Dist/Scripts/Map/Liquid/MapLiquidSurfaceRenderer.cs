// ============================================================
// MapLiquidSurfaceRenderer — 로드된 청크의 액체 수면을 청크 메시로 그린다
// ============================================================
// 정적 셀 무연산 보증(docs/map/LIQUID.md §3):
// - 오버레이를 매 프레임 순회하지 않는다. 물이 있는 청크와 그 층 범위만 들고 있다가
//   CellChanged 통지를 받은 청크만 다시 굽는다. 정지한 바다는 첫 메시 이후 리메시가 0이다.
// - 매 프레임 도는 것은 "로드된 청크"뿐이다(스트리밍 반경 ≈ 수십 개). 맵 전체 물 청크가 아니다.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace IsoTilemap
{
    [DisallowMultipleComponent]
    public sealed class MapLiquidSurfaceRenderer : MonoBehaviour
    {
        [Tooltip("비우면 Dist/MapLiquidSurface 셰이더로 런타임 폴백 머티리얼을 만든다(빌드에서는 반드시 지정).")]
        [SerializeField] Material _surfaceMaterial;

        [Tooltip("수면 메시를 그릴 레이어 인덱스. 카메라 culling mask에 포함되어 있어야 한다.")]
        [SerializeField] int _layer;

        static readonly int GlobalTimeId = Shader.PropertyToID(MapLiquidRenderConsts.GlobalTimeProperty);

        readonly MapLiquidChunkMesher _mesher = new();
        readonly Dictionary<Vector2Int, ChunkEntry> _chunks = new();
        readonly Dictionary<Vector2Int, ChunkYRange> _waterChunks = new();
        readonly HashSet<Vector2Int> _dirtySet = new();
        readonly Queue<Vector2Int> _dirtyQueue = new();
        readonly HashSet<Vector2Int> _renderChunks = new();
        readonly List<Vector2Int> _loadedScratch = new();
        readonly List<Vector2Int> _releaseScratch = new();

        MapLiquidOverlay _overlay;
        TileMapStreamingVisualizer _streaming;
        Material _resolvedMaterial;
        Material _runtimeMaterial;
        float _cellSize = 1f;
        int _chunkSize = MapLiquidRenderConsts.FallbackChunkSize;
        bool _bulkDirty;
        bool _materialResolveFailed;

        public void Bind(MapLiquidOverlay overlay)
        {
            Unsubscribe();
            _overlay = overlay;
            if (_overlay == null)
                return;

            _overlay.CellChanged += OnCellChanged;
            _overlay.BulkChanged += OnBulkChanged;
            _mesher.Bind(_overlay, _cellSize);
            _bulkDirty = true;
        }

        public void BindMapContext(float cellSize, int chunkSize, TileMapStreamingVisualizer streaming)
        {
            _cellSize = Mathf.Max(1e-4f, cellSize);
            _chunkSize = chunkSize > 0 ? chunkSize : MapLiquidRenderConsts.FallbackChunkSize;
            _streaming = streaming;
            _materialResolveFailed = false;
            _resolvedMaterial = null;
            _mesher.Bind(_overlay, _cellSize);
            _bulkDirty = true;
        }

        void OnDestroy()
        {
            Unsubscribe();
            ReleaseAllMeshes();
            DestroySafely(_runtimeMaterial);
            _runtimeMaterial = null;
            _resolvedMaterial = null;
        }

        void Unsubscribe()
        {
            if (_overlay == null)
                return;

            _overlay.CellChanged -= OnCellChanged;
            _overlay.BulkChanged -= OnBulkChanged;
        }

        void OnCellChanged(Vector3Int cell)
        {
            if (_bulkDirty)
                return;

            Vector2Int chunk = TileChunkCoord.FromCell(cell, _chunkSize);
            TouchWaterChunk(chunk, cell.y);
            MarkDirty(chunk);

            // 코너 높이는 이웃 셀에서도 읽으므로, 경계 셀이 바뀌면 맞닿은 청크도 다시 구워야 한다.
            // 모서리 셀의 코너는 대각 청크와도 공유되므로 대각까지 포함한다.
            int localX = cell.x - chunk.x * _chunkSize;
            int localZ = cell.z - chunk.y * _chunkSize;
            int stepX = localX == 0 ? -1 : localX == _chunkSize - 1 ? 1 : 0;
            int stepZ = localZ == 0 ? -1 : localZ == _chunkSize - 1 ? 1 : 0;

            if (stepX != 0)
                MarkNeighborDirty(chunk, stepX, 0, cell.y);
            if (stepZ != 0)
                MarkNeighborDirty(chunk, 0, stepZ, cell.y);
            if (stepX != 0 && stepZ != 0)
                MarkNeighborDirty(chunk, stepX, stepZ, cell.y);
        }

        void MarkNeighborDirty(Vector2Int chunk, int dx, int dz, int cellY)
        {
            var neighbor = new Vector2Int(chunk.x + dx, chunk.y + dz);
            if (!_waterChunks.ContainsKey(neighbor))
                return;

            TouchWaterChunk(neighbor, cellY);
            MarkDirty(neighbor);
        }

        void TouchWaterChunk(Vector2Int chunk, int cellY)
        {
            if (_waterChunks.TryGetValue(chunk, out ChunkYRange range))
                _waterChunks[chunk] = range.Expanded(cellY);
            else
                _waterChunks[chunk] = new ChunkYRange(cellY, cellY);
        }

        void MarkDirty(Vector2Int chunk)
        {
            if (_dirtySet.Add(chunk))
                _dirtyQueue.Enqueue(chunk);
        }

        /// <summary>대량 교체는 프레임당 1회로 합친다 — 맵 1회 로드에서 Clear/Load가 여러 번 통지해도 재구성은 한 번.</summary>
        void OnBulkChanged() => _bulkDirty = true;

        void RebuildWaterChunkIndex()
        {
            _bulkDirty = false;
            ReleaseAllMeshes();
            _waterChunks.Clear();
            _dirtySet.Clear();
            _dirtyQueue.Clear();

            if (_overlay == null)
                return;

            foreach (var kv in _overlay.Cells)
            {
                Vector3Int cell = kv.Key;
                TouchWaterChunk(TileChunkCoord.FromCell(cell, _chunkSize), cell.y);
            }
        }

        void LateUpdate()
        {
            if (_overlay == null || _materialResolveFailed)
                return;

            if (_bulkDirty)
                RebuildWaterChunkIndex();

            // 해제는 물 청크가 0개가 된 프레임에도 돌아야 한다 — 마지막 청크의 Mesh가 남지 않도록.
            BuildRenderSet();
            ReleaseUnrenderedChunks();

            Material material = ResolveMaterial();
            if (material == null)
                return;

            Shader.SetGlobalFloat(GlobalTimeId, ResolveShaderTime());

            if (_renderChunks.Count == 0)
                return;

            ProcessDirtyQueue();
            DrawRenderSet(material);
        }

        /// <summary>수면 애니메이션 시간. 누적 초를 그대로 넘기면 노이즈 해시의 float 정밀도가 무너지므로 감는다.</summary>
        static float ResolveShaderTime()
        {
            float now = TimeScaleService.TimeNow(TimeScaleChannel.World);
            return Mathf.Repeat(now, MapLiquidRenderConsts.ShaderTimeWrapSeconds);
        }

        void BuildRenderSet()
        {
            _renderChunks.Clear();

            if (_streaming == null)
            {
                foreach (var kv in _waterChunks)
                    _renderChunks.Add(kv.Key);

                return;
            }

            // 로드된 청크(수십 개)를 기준으로 교집합을 잡는다 — 맵 전체 물 청크 수와 무관한 비용.
            _streaming.CollectLoadedChunks(_loadedScratch);
            for (int i = 0; i < _loadedScratch.Count; i++)
            {
                Vector2Int chunk = _loadedScratch[i];
                if (_waterChunks.ContainsKey(chunk))
                    _renderChunks.Add(chunk);
            }
        }

        /// <summary>언로드되었거나 물이 사라진 청크의 메시를 반납한다.</summary>
        void ReleaseUnrenderedChunks()
        {
            if (_chunks.Count == 0)
                return;

            _releaseScratch.Clear();
            foreach (var kv in _chunks)
            {
                if (!_renderChunks.Contains(kv.Key))
                    _releaseScratch.Add(kv.Key);
            }

            for (int i = 0; i < _releaseScratch.Count; i++)
                ReleaseChunkMesh(_releaseScratch[i]);
        }

        /// <summary>FIFO — 예산을 넘겨도 뒤쪽 청크가 굶지 않는다.</summary>
        void ProcessDirtyQueue()
        {
            int budget = MapLiquidRenderConsts.MaxChunkRemeshPerFrame;
            int pending = _dirtyQueue.Count;

            while (budget > 0 && pending > 0)
            {
                pending--;
                Vector2Int chunk = _dirtyQueue.Dequeue();
                _dirtySet.Remove(chunk);

                // 화면 밖 청크는 다시 로드될 때 새로 구우므로 여기서 예산을 쓰지 않는다.
                if (!_renderChunks.Contains(chunk) || !_chunks.TryGetValue(chunk, out ChunkEntry entry))
                    continue;

                RebuildChunk(chunk, entry);
                budget--;
            }
        }

        void DrawRenderSet(Material material)
        {
            var renderParams = new RenderParams(material)
            {
                layer = _layer,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
            };

            int buildBudget = MapLiquidRenderConsts.MaxChunkBuildPerFrame;

            foreach (Vector2Int chunk in _renderChunks)
            {
                if (!_chunks.TryGetValue(chunk, out ChunkEntry entry))
                {
                    if (buildBudget <= 0)
                        continue;

                    entry = CreateEntry(chunk);
                    RebuildChunk(chunk, entry);
                    buildBudget--;
                }

                if (!entry.HasGeometry)
                    continue;

                renderParams.worldBounds = entry.WorldBounds;
                Graphics.RenderMesh(in renderParams, entry.Mesh, 0, entry.LocalToWorld);
            }
        }

        ChunkEntry CreateEntry(Vector2Int chunk)
        {
            Vector3 origin = ChunkOrigin(chunk);
            var entry = new ChunkEntry(
                new Mesh { name = "MapLiquidChunk", hideFlags = HideFlags.DontSave },
                Matrix4x4.TRS(origin, Quaternion.identity, Vector3.one),
                origin);
            _chunks[chunk] = entry;
            return entry;
        }

        void RebuildChunk(Vector2Int chunk, ChunkEntry entry)
        {
            if (!_waterChunks.TryGetValue(chunk, out ChunkYRange range))
            {
                entry.SetEmpty();
                return;
            }

            MapLiquidChunkMesher.BuildResult result = _mesher.Build(
                entry.Mesh,
                chunk,
                _chunkSize,
                range.MinY,
                range.MaxY,
                entry.Origin);

            entry.SetGeometry(result.HasGeometry);

            // 물이 남아 있는지로 판정한다. 노출면이 없어 삼각형이 0개인 내부 층을 물이 마른 것으로
            // 오해하면 그 청크가 목록에서 빠져 아래 층 수면을 영영 못 만든다.
            if (!result.HasLiquid)
            {
                _waterChunks.Remove(chunk);
                return;
            }

            _waterChunks[chunk] = new ChunkYRange(result.MinY, result.MaxY);
        }

        void ReleaseChunkMesh(Vector2Int chunk)
        {
            if (!_chunks.TryGetValue(chunk, out ChunkEntry entry))
                return;

            DestroySafely(entry.Mesh);
            _chunks.Remove(chunk);
        }

        Vector3 ChunkOrigin(Vector2Int chunk) => new(
            chunk.x * _chunkSize * _cellSize,
            0f,
            chunk.y * _chunkSize * _cellSize);

        Material ResolveMaterial()
        {
            if (_surfaceMaterial != null)
                return _surfaceMaterial;

            if (_resolvedMaterial != null)
                return _resolvedMaterial;

            _resolvedMaterial = Resources.Load<Material>(MapLiquidRenderConsts.SurfaceMaterialResourcePath);
            if (_resolvedMaterial != null)
                return _resolvedMaterial;

            Shader shader = Shader.Find(MapLiquidRenderConsts.SurfaceShaderName);
            if (shader != null)
            {
                _runtimeMaterial = new Material(shader)
                {
                    name = "MapLiquidSurface (Runtime)",
                    hideFlags = HideFlags.DontSave,
                };
                _resolvedMaterial = _runtimeMaterial;
                return _resolvedMaterial;
            }

            // 그릴 수 없는 상태로 통지만 계속 받으면 dirty 큐가 무한히 자란다 — 아예 손을 뗀다.
            _materialResolveFailed = true;
            Unsubscribe();
            ReleaseAllMeshes();
            _waterChunks.Clear();
            _dirtySet.Clear();
            _dirtyQueue.Clear();
            _renderChunks.Clear();
            Debug.LogError(
                "[MapLiquidSurfaceRenderer] 수면 머티리얼을 찾지 못해 렌더를 중단합니다. Inspector에 지정하거나 " +
                $"Resources/{MapLiquidRenderConsts.SurfaceMaterialResourcePath}.mat 를 두세요.",
                this);
            return null;
        }

        void ReleaseAllMeshes()
        {
            foreach (var kv in _chunks)
                DestroySafely(kv.Value.Mesh);

            _chunks.Clear();
        }

        static void DestroySafely(Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        readonly struct ChunkYRange
        {
            public readonly int MinY;
            public readonly int MaxY;

            public ChunkYRange(int minY, int maxY)
            {
                MinY = minY;
                MaxY = maxY;
            }

            public ChunkYRange Expanded(int cellY) => new(
                cellY < MinY ? cellY : MinY,
                cellY > MaxY ? cellY : MaxY);
        }

        sealed class ChunkEntry
        {
            public readonly Mesh Mesh;
            public readonly Matrix4x4 LocalToWorld;
            public readonly Vector3 Origin;

            public bool HasGeometry { get; private set; }
            public Bounds WorldBounds { get; private set; }

            public ChunkEntry(Mesh mesh, Matrix4x4 localToWorld, Vector3 origin)
            {
                Mesh = mesh;
                LocalToWorld = localToWorld;
                Origin = origin;
            }

            public void SetGeometry(bool hasGeometry)
            {
                HasGeometry = hasGeometry;
                if (!hasGeometry)
                    return;

                Bounds local = Mesh.bounds;
                WorldBounds = new Bounds(local.center + Origin, local.size);
            }

            public void SetEmpty() => HasGeometry = false;
        }
    }
}
