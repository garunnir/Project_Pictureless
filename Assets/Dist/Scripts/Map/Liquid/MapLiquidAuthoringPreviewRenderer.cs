#if UNITY_EDITOR
// ============================================================
// MapLiquidAuthoringPreviewRenderer — 에디터 물 저작 merged mesh 프리뷰
// ============================================================
// 씬 LiquidAuthoringView → cap-full synthetic overlay → MapLiquidChunkMesher(Play와 동일).
// 인접 셀은 내부 면을 생략해 청크 단일 메시로 그린다. sim·MapLiquidHost 없음.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace IsoTilemap
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class MapLiquidAuthoringPreviewRenderer : MonoBehaviour
    {
        static readonly int GlobalTimeId = Shader.PropertyToID(MapLiquidRenderConsts.GlobalTimeProperty);

        static MapLiquidAuthoringPreviewRenderer _instance;

        readonly MapLiquidOverlay _overlay = new();
        readonly MapLiquidChunkMesher _mesher = new();
        readonly Dictionary<Vector2Int, ChunkEntry> _chunks = new();
        readonly Dictionary<Vector2Int, ChunkYRange> _waterChunks = new();

        float _cellSize = 1f;
        int _chunkSize = MapLiquidRenderConsts.FallbackChunkSize;
        Material _resolvedMaterial;
        Material _runtimeMaterial;
        bool _rebuildRequested = true;
        bool _materialResolveFailed;

        public static void RequestRefresh()
        {
            if (_instance != null)
                _instance._rebuildRequested = true;
        }

        public void Bind(float cellSize, int chunkSize)
        {
            _cellSize = Mathf.Max(1e-4f, cellSize);
            _chunkSize = chunkSize > 0 ? chunkSize : MapLiquidRenderConsts.FallbackChunkSize;
            _mesher.Bind(_overlay, _cellSize);
            _rebuildRequested = true;
        }

        public void RebuildFromScene()
        {
            _rebuildRequested = false;
            ReleaseAllMeshes();
            _waterChunks.Clear();
            _overlay.Clear();

            LiquidAuthoringView[] views = Object.FindObjectsByType<LiquidAuthoringView>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < views.Length; i++)
            {
                LiquidAuthoringView view = views[i];
                if (view == null)
                    continue;

                if (view.IsAuthoringTarget())
                {
                    _overlay.SeedEffectiveMl(
                        view.LiquidCell,
                        MapLiquidConsts.WaterTypeId,
                        MapLiquidConsts.DefaultMaxVolumeMl);
                    SetMarkerMeshVisible(view, false);
                }
                else
                {
                    SetMarkerMeshVisible(view, true);
                }
            }

            foreach (var kv in _overlay.Cells)
            {
                Vector3Int cell = kv.Key;
                TouchWaterChunk(TileChunkCoord.FromCell(cell, _chunkSize), cell.y);
            }

            foreach (Vector2Int chunk in new List<Vector2Int>(_waterChunks.Keys))
            {
                ChunkEntry entry = CreateEntry(chunk);
                RebuildChunk(chunk, entry);
            }
        }

        void OnEnable()
        {
            _instance = this;
            _rebuildRequested = true;
        }

        void OnDisable()
        {
            if (_instance == this)
                _instance = null;

            ReleaseAllMeshes();
            RestoreAllMarkerMeshes();
        }

        void LateUpdate()
        {
            if (Application.isPlaying)
                return;

            if (_rebuildRequested)
                RebuildFromScene();

            if (_materialResolveFailed || _waterChunks.Count == 0)
                return;

            Material material = ResolveMaterial();
            if (material == null)
                return;

            Shader.SetGlobalFloat(
                GlobalTimeId,
                Mathf.Repeat((float)EditorApplication.timeSinceStartup, MapLiquidRenderConsts.ShaderTimeWrapSeconds));

            DrawAllChunks(material);
        }

        void TouchWaterChunk(Vector2Int chunk, int cellY)
        {
            if (_waterChunks.TryGetValue(chunk, out ChunkYRange range))
                _waterChunks[chunk] = range.Expanded(cellY);
            else
                _waterChunks[chunk] = new ChunkYRange(cellY, cellY);
        }

        void DrawAllChunks(Material material)
        {
            var renderParams = new RenderParams(material)
            {
                layer = gameObject.layer,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
            };

            foreach (var kv in _chunks)
            {
                ChunkEntry entry = kv.Value;
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
                new Mesh { name = "MapLiquidAuthoringPreviewChunk", hideFlags = HideFlags.DontSave },
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

            if (!result.HasLiquid)
            {
                _waterChunks.Remove(chunk);
                ReleaseChunkMesh(chunk);
                return;
            }

            _waterChunks[chunk] = new ChunkYRange(result.MinY, result.MaxY);
        }

        Vector3 ChunkOrigin(Vector2Int chunk) => new(
            chunk.x * _chunkSize * _cellSize,
            0f,
            chunk.y * _chunkSize * _cellSize);

        Material ResolveMaterial()
        {
            if (_resolvedMaterial != null)
                return _resolvedMaterial;

            _resolvedMaterial = Resources.Load<Material>(MapLiquidRenderConsts.SurfaceMaterialResourcePath);
            if (_resolvedMaterial != null)
                return _resolvedMaterial;

            _resolvedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Dist/Resources/Map/MapLiquidSurface.mat");
            if (_resolvedMaterial != null)
                return _resolvedMaterial;

            Shader shader = Shader.Find(MapLiquidRenderConsts.SurfaceShaderName);
            if (shader != null)
            {
                _runtimeMaterial = new Material(shader)
                {
                    name = "MapLiquidSurface (Authoring Preview)",
                    hideFlags = HideFlags.DontSave,
                };
                _resolvedMaterial = _runtimeMaterial;
                return _resolvedMaterial;
            }

            if (!_materialResolveFailed)
            {
                _materialResolveFailed = true;
                Debug.LogError(
                    "[MapLiquidAuthoringPreviewRenderer] 수면 머티리얼을 찾지 못했습니다. " +
                    $"Resources/{MapLiquidRenderConsts.SurfaceMaterialResourcePath}.mat 를 확인하세요.",
                    this);
            }

            return null;
        }

        static void SetMarkerMeshVisible(LiquidAuthoringView view, bool visible)
        {
            MeshRenderer[] renderers = view.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = visible;
        }

        static void RestoreAllMarkerMeshes()
        {
            LiquidAuthoringView[] views = Object.FindObjectsByType<LiquidAuthoringView>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] != null)
                    SetMarkerMeshVisible(views[i], true);
            }
        }

        void ReleaseChunkMesh(Vector2Int chunk)
        {
            if (!_chunks.TryGetValue(chunk, out ChunkEntry entry))
                return;

            DestroyImmediate(entry.Mesh);
            _chunks.Remove(chunk);
        }

        void ReleaseAllMeshes()
        {
            foreach (var kv in _chunks)
                DestroyImmediate(kv.Value.Mesh);

            _chunks.Clear();
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
#endif
