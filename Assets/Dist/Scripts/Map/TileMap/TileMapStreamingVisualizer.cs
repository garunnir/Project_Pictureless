using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// 청크 단위 타일 스트리밍 뷰. 모델은 전체 유지, GameObject는 desired 청크만 스폰.
    /// </summary>
    public sealed class TileMapStreamingVisualizer : IMapViewBuilder, ITileViewRegistry, IDisposable
    {
        private readonly TileObjFactory _tileFactory;
        private readonly IWorldGrid _worldGrid;
        private readonly int _chunkSize;
        private readonly bool _ownsChunkIndex;

        private readonly Dictionary<Guid, TileView> _tileViews = new();
        private readonly HashSet<Vector2Int> _loadedChunks = new();
        private readonly Dictionary<Guid, HashSet<Vector2Int>> _tileChunkRefs = new();

        private readonly List<TileData> _gatherBuffer = new();
        private readonly List<Vector2Int> _chunkIteration = new();
        private readonly HashSet<Guid> _orphanCheckIds = new();
        private readonly List<Guid> _pruneGuids = new();
        private readonly HashSet<int> _buildingsToDespawnScratch = new();
        private readonly OcclusionModeController _buildingOcclusionController = new();

        private TileMapChunkIndex _chunkIndex;
        private BuildingGroupRegistry _buildingRegistry;
        private IMapModelReadOnly _boundRuntime;
        private PlayerFloorVisibilityPolicy _floorPolicy;
        private FloorVisibilityContext _floorContext;
        private bool _hasFloorContext;
        private TileViewPresentationApplier _presentationApplier;

        public TileMapStreamingVisualizer(
            TileObjFactory tileFactory,
            IWorldGrid worldGrid,
            int chunkSize = 16,
            TileMapChunkIndex sharedChunkIndex = null)
        {
            _tileFactory = tileFactory;
            _worldGrid = worldGrid;
            _chunkSize = Mathf.Max(1, chunkSize);
            _chunkIndex = sharedChunkIndex;
            _ownsChunkIndex = sharedChunkIndex == null;
        }

        private float CellSize => _worldGrid != null ? _worldGrid.CellSize : 1f;

        public TileMapChunkIndex ChunkIndex => _chunkIndex;

        public IReadOnlyCollection<Vector2Int> LoadedChunks => _loadedChunks;

        public void SetFloorVisibilityPolicy(PlayerFloorVisibilityPolicy policy) => _floorPolicy = policy;

        public void SetPresentationApplier(TileViewPresentationApplier applier) => _presentationApplier = applier;

        public void SetBuildingRegistry(BuildingGroupRegistry buildingRegistry) =>
            _buildingRegistry = buildingRegistry;

        public bool TryGetView(Guid tileId, out TileView view) => _tileViews.TryGetValue(tileId, out view);

        public void SyncFloorVisibility(in FloorVisibilityContext ctx)
        {
            _floorContext = ctx;
            _hasFloorContext = _floorPolicy != null;
            _presentationApplier?.SetOcclusionMode(ctx.OcclusionMode);

            ApplyBlockingBuildingDelta(in ctx);

            if (_chunkIndex == null || _loadedChunks.Count == 0)
                return;

            _chunkIteration.Clear();
            _chunkIteration.AddRange(_loadedChunks);
            for (int c = 0; c < _chunkIteration.Count; c++)
            {
                IReadOnlyList<Vector3Int> cells = _chunkIndex.GetCellsInChunk(_chunkIteration[c]);
                for (int i = 0; i < cells.Count; i++)
                    RefreshCellAtLoadedChunk(cells[i]);
            }
        }

        public void Build(IMapModelReadOnly model)
        {
            ClearAllTiles();
            _loadedChunks.Clear();
            _tileChunkRefs.Clear();

            if (_chunkIndex == null)
                _chunkIndex = new TileMapChunkIndex();

            _chunkIndex.Build(model, _chunkSize);
        }

        public void Bind(IMapModelReadOnly runtime)
        {
            if (_boundRuntime != null)
            {
                _boundRuntime.OnRuntimeDataChanged -= RefreshCell;
                _boundRuntime.OnRuntimeBatchChanged -= RefreshCells;
                _boundRuntime.OnRuntimeTileAdded -= OnRuntimeTileAdded;
                _boundRuntime.OnRuntimeTileRemoved -= OnRuntimeTileRemoved;
            }

            _boundRuntime = runtime;

            if (_boundRuntime != null)
            {
                _boundRuntime.OnRuntimeDataChanged += RefreshCell;
                _boundRuntime.OnRuntimeBatchChanged += RefreshCells;
                _boundRuntime.OnRuntimeTileAdded += OnRuntimeTileAdded;
                _boundRuntime.OnRuntimeTileRemoved += OnRuntimeTileRemoved;
            }
        }

        public void SyncDesiredChunks(HashSet<Vector2Int> desired)
        {
            if (_chunkIndex == null || desired == null)
                return;

            _chunkIteration.Clear();
            _chunkIteration.AddRange(_loadedChunks);
            for (int i = 0; i < _chunkIteration.Count; i++)
            {
                Vector2Int chunk = _chunkIteration[i];
                if (!desired.Contains(chunk))
                    UnloadChunk(chunk);
            }

            foreach (var chunk in desired)
            {
                if (!_loadedChunks.Contains(chunk))
                    LoadChunk(chunk);
            }
        }

        public void RefreshCell(Vector3Int cellPos, IReadOnlyList<TileData> tiles)
        {
            if (!IsCellInLoadedChunk(cellPos))
                return;

            if (_boundRuntime != null)
            {
                RefreshCellAtLoadedChunk(cellPos);
                return;
            }

            RenderTiles(tiles);
        }

        private void RefreshCellAtLoadedChunk(Vector3Int cellPos)
        {
            if (_boundRuntime == null || !IsCellInLoadedChunk(cellPos))
                return;

            GatherAndFilter(cellPos, _gatherBuffer);
            PruneOrphanViewsAtCell(cellPos, _gatherBuffer);
            RenderTiles(_gatherBuffer);
        }

        private void GatherAndFilter(Vector3Int cellPos, List<TileData> buffer)
        {
            _boundRuntime.GatherRenderableTiles(cellPos, buffer);
            if (_hasFloorContext && _floorPolicy != null)
                _floorPolicy.FilterTiles(buffer, _floorContext);
        }

        public void Dispose()
        {
            if (_boundRuntime != null)
            {
                _boundRuntime.OnRuntimeDataChanged -= RefreshCell;
                _boundRuntime.OnRuntimeBatchChanged -= RefreshCells;
                _boundRuntime.OnRuntimeTileAdded -= OnRuntimeTileAdded;
                _boundRuntime.OnRuntimeTileRemoved -= OnRuntimeTileRemoved;
                _boundRuntime = null;
            }

            ClearAllTiles();
            _loadedChunks.Clear();
            _tileChunkRefs.Clear();

            if (_ownsChunkIndex)
                _chunkIndex = null;
        }

        private void OnRuntimeTileAdded(TileData tileData)
        {
            _chunkIndex?.RegisterTile(tileData, _chunkSize);
        }

        private void OnRuntimeTileRemoved(TileData tileData)
        {
            _chunkIndex?.UnregisterTile(tileData, _chunkSize);
            DespawnViewCompletely(tileData.tileDefId);
        }

        private void RefreshCells(IReadOnlyCollection<Vector3Int> changedCells)
        {
            if (_boundRuntime == null)
                return;

            foreach (var cellPos in changedCells)
            {
                if (!IsCellInLoadedChunk(cellPos))
                    continue;

                GatherAndFilter(cellPos, _gatherBuffer);
                if (_gatherBuffer.Count > 0)
                {
                    PruneOrphanViewsAtCell(cellPos, _gatherBuffer);
                    RenderTiles(_gatherBuffer);
                }
                else
                    PruneOrphanViewsAtCell(cellPos, _gatherBuffer);
            }
        }

        private void LoadChunk(Vector2Int chunk)
        {
            if (!_loadedChunks.Add(chunk))
                return;

            IReadOnlyList<Vector3Int> cells = _chunkIndex.GetCellsInChunk(chunk);
            for (int i = 0; i < cells.Count; i++)
            {
                if (_boundRuntime == null)
                    continue;

                GatherAndFilter(cells[i], _gatherBuffer);
                for (int t = 0; t < _gatherBuffer.Count; t++)
                    AcquireTileInChunk(_gatherBuffer[t], chunk);
            }
        }

        private void UnloadChunk(Vector2Int chunk)
        {
            if (!_loadedChunks.Remove(chunk))
                return;

            _pruneGuids.Clear();
            foreach (var kv in _tileChunkRefs)
            {
                if (kv.Value.Contains(chunk))
                    _pruneGuids.Add(kv.Key);
            }

            for (int i = 0; i < _pruneGuids.Count; i++)
                ReleaseTileFromChunk(_pruneGuids[i], chunk);
        }

        private void AcquireTileInChunk(TileData tileData, Vector2Int chunk)
        {
            Guid id = tileData.tileDefId;
            if (!_tileChunkRefs.TryGetValue(id, out HashSet<Vector2Int> refs))
            {
                refs = new HashSet<Vector2Int>();
                _tileChunkRefs[id] = refs;
            }

            refs.Add(chunk);

            if (!_tileViews.TryGetValue(id, out TileView view))
            {
                view = _tileFactory.SpawnTile(tileData, CellSize);
                if (view == null)
                {
                    refs.Remove(chunk);
                    if (refs.Count == 0)
                        _tileChunkRefs.Remove(id);
                    return;
                }

                _tileViews[id] = view;
                _presentationApplier?.SyncPresentationForTile(id);
                ApplyBlockingModeForView(tileData, view);
                return;
            }

            view.UpdateTile(tileData, CellSize);
            _presentationApplier?.SyncPresentationForTile(id);
            ApplyBlockingModeForView(tileData, view);
        }

        private void ReleaseTileFromChunk(Guid tileId, Vector2Int chunk)
        {
            if (!_tileChunkRefs.TryGetValue(tileId, out HashSet<Vector2Int> refs))
                return;

            refs.Remove(chunk);
            if (refs.Count > 0)
                return;

            _tileChunkRefs.Remove(tileId);
            if (_tileViews.TryGetValue(tileId, out TileView view))
            {
                _tileFactory.DespawnTile(view);
                _tileViews.Remove(tileId);
            }
        }

        private void RenderTiles(IReadOnlyList<TileData> tiles)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                TileData tileData = tiles[i];
                if (_tileViews.TryGetValue(tileData.tileDefId, out TileView tileView))
                {
                    tileView.UpdateTile(tileData, CellSize);
                    ApplyBlockingModeForView(tileData, tileView);
                }
                else if (IsTileInLoadedChunk(tileData))
                {
                    AcquireTileInChunk(tileData, TileChunkCoord.FromCell(GetRepresentativeCell(tileData), _chunkSize));
                    _presentationApplier?.SyncPresentationForTile(tileData.tileDefId);
                }
            }
        }

        private void PruneOrphanViewsAtCell(Vector3Int cell, List<TileData> currentTiles)
        {
            _orphanCheckIds.Clear();
            for (int i = 0; i < currentTiles.Count; i++)
                _orphanCheckIds.Add(currentTiles[i].tileDefId);

            _pruneGuids.Clear();
            _buildingsToDespawnScratch.Clear();
            foreach (var kv in _tileViews)
            {
                if (_orphanCheckIds.Contains(kv.Key))
                    continue;

                if (!ViewTouchesCell(kv.Value, cell))
                    continue;

                if (TryGetBlockingBuildingIdForView(kv.Key, out int buildingId))
                {
                    _buildingsToDespawnScratch.Add(buildingId);
                    continue;
                }

                _pruneGuids.Add(kv.Key);
            }

            foreach (int buildingId in _buildingsToDespawnScratch)
                DespawnAllLoadedViewsForBuilding(buildingId, in _floorContext);

            for (int i = 0; i < _pruneGuids.Count; i++)
                DespawnViewCompletely(_pruneGuids[i]);
        }

        void ApplyBlockingBuildingDelta(in FloorVisibilityContext ctx)
        {
            HashSet<int> newBlocking = ctx.PlayerBlockingBuildingIds;
            _buildingOcclusionController.ApplyDelta(
                newBlocking,
                ctx.OcclusionMode,
                ctx,
                OnBlockingAdded,
                OnBlockingRemoved);

            if (_presentationApplier != null &&
                (_buildingOcclusionController.LastAdded.Count > 0 || _buildingOcclusionController.LastRemoved.Count > 0))
            {
                var delta = new BuildingSightLinePresentationDelta(
                    _buildingOcclusionController.LastAdded,
                    _buildingOcclusionController.LastRemoved);
                _presentationApplier.ApplySightLineBlockingDelta(in delta);
            }
        }

        void OnBlockingAdded(int buildingId, OcclusionMode mode, FloorVisibilityContext ctx)
        {
            switch (ResolveBuildingApplyMode(mode))
            {
                case BuildingApplyMode.FullDespawn:
                    DespawnAllLoadedViewsForBuilding(buildingId, in ctx);
                    break;
                case BuildingApplyMode.RenderOnly:
                case BuildingApplyMode.ColliderOnly:
                    SetBuildingViewsOccluded(buildingId, true, mode);
                    break;
                case BuildingApplyMode.AlphaBlend:
                    SetBuildingViewsAlphaBlend(buildingId, true);
                    break;
            }
        }

        void OnBlockingRemoved(int buildingId, OcclusionMode mode, FloorVisibilityContext ctx)
        {
            switch (ResolveBuildingApplyMode(mode))
            {
                case BuildingApplyMode.FullDespawn:
                    RespawnVisibleBuildingTiles(buildingId, in ctx);
                    break;
                case BuildingApplyMode.RenderOnly:
                case BuildingApplyMode.ColliderOnly:
                    SetBuildingViewsOccluded(buildingId, false, mode);
                    break;
                case BuildingApplyMode.AlphaBlend:
                    SetBuildingViewsAlphaBlend(buildingId, false);
                    break;
            }
        }

        void DespawnAllLoadedViewsForBuilding(int buildingId, in FloorVisibilityContext ctx)
        {
            if (_buildingRegistry == null || buildingId <= 0)
                return;

            foreach (Guid tileId in _buildingRegistry.GetTilesForBuilding(buildingId))
            {
                if (!_tileViews.ContainsKey(tileId))
                    continue;

                if (_boundRuntime != null &&
                    _boundRuntime.TryGetTileById(tileId, out TileData tile) &&
                    IsMinBandFloorTile(tile, ctx.MinBand))
                    continue;

                DespawnViewCompletely(tileId);
            }
        }

        void RespawnVisibleBuildingTiles(int buildingId, in FloorVisibilityContext ctx)
        {
            if (_buildingRegistry == null || buildingId <= 0 || _boundRuntime == null || _floorPolicy == null)
                return;

            foreach (Guid tileId in _buildingRegistry.GetTilesForBuilding(buildingId))
            {
                if (!_boundRuntime.TryGetTileById(tileId, out TileData tile))
                    continue;

                if (!_floorPolicy.IsTileVisible(tile, in ctx))
                    continue;

                if (!IsTileInLoadedChunk(tile))
                    continue;

                Vector2Int chunk = _chunkIndex != null &&
                                   _chunkIndex.TryGetChunkForTile(tileId, out Vector2Int c)
                    ? c
                    : TileChunkCoord.FromCell(GetRepresentativeCell(tile), _chunkSize);

                AcquireTileInChunk(tile, chunk);
            }
        }

        void SetBuildingViewsOccluded(int buildingId, bool hidden, OcclusionMode mode)
        {
            if (_buildingRegistry == null || buildingId <= 0)
                return;

            foreach (Guid tileId in _buildingRegistry.GetTilesForBuilding(buildingId))
            {
                if (_tileViews.TryGetValue(tileId, out TileView view))
                    SetViewOcclusionState(view, hidden, mode);
            }
        }

        void SetBuildingViewsAlphaBlend(int buildingId, bool hidden)
        {
            if (_buildingRegistry == null || buildingId <= 0)
                return;

            foreach (Guid tileId in _buildingRegistry.GetTilesForBuilding(buildingId))
            {
                if (!_tileViews.TryGetValue(tileId, out TileView view))
                    continue;

                if (!IsStructuralWallView(view))
                    continue;

                if (hidden)
                    view.ApplyWallOcclusionMode(1f, OcclusionMode.AlphaBlendPreserve);
                else
                    view.ApplyWallOcclusionMode(0f, OcclusionMode.AlphaBlendPreserve);
            }
        }

        static bool IsStructuralWallView(TileView view)
        {
            if (view == null)
                return false;

            var type = view.tileType;
            return type == TileView.TileType.Wall || type == TileView.TileType.EdgeWall;
        }

        bool TryGetBlockingBuildingIdForView(Guid tileId, out int buildingId)
        {
            buildingId = 0;
            if (!_buildingOcclusionController.HasAnyBlocked)
                return false;

            if (_boundRuntime == null || !_boundRuntime.TryGetTileById(tileId, out TileData tile))
                return false;

            buildingId = tile.identity.buildingId;
            return buildingId > 0 && _buildingOcclusionController.IsBlocked(buildingId);
        }

        static bool IsMinBandFloorTile(TileData tile, int minBand) =>
            tile.identity.GridPos.y == minBand &&
            (TileView.TileType)tile.identity.tileType == TileView.TileType.Floor;

        private static bool ViewTouchesCell(TileView view, Vector3Int cell)
        {
            if (view == null)
                return false;

            if (view.tileType == TileView.TileType.EdgeWall)
            {
                var key = new WallEdgeKey(view.gridPos, (WallFace)view.wallEdgeFace);
                return key.Anchor == cell || key.NeighborCell() == cell;
            }

            return view.gridPos == cell;
        }

        private void DespawnViewCompletely(Guid tileId)
        {
            _tileChunkRefs.Remove(tileId);
            if (!_tileViews.TryGetValue(tileId, out TileView view))
                return;

            _tileFactory.DespawnTile(view);
            _tileViews.Remove(tileId);
        }

        private bool IsCellInLoadedChunk(Vector3Int cell) =>
            _loadedChunks.Contains(TileChunkCoord.FromCell(cell, _chunkSize));

        private bool IsTileInLoadedChunk(TileData tileData)
        {
            if (_chunkIndex != null &&
                _chunkIndex.TryGetChunkForTile(tileData.tileDefId, out Vector2Int chunk))
                return _loadedChunks.Contains(chunk);

            return IsCellInLoadedChunk(GetRepresentativeCell(tileData));
        }

        private static Vector3Int GetRepresentativeCell(TileData tileData)
        {
            if ((TileView.TileType)tileData.identity.tileType == TileView.TileType.EdgeWall)
                return WallEdgeKey.FromEdgeTileIdentity(tileData.identity).Anchor;

            return tileData.identity.GridPos;
        }

        private void ClearAllTiles()
        {
            foreach (var view in _tileViews.Values)
            {
                if (view != null)
                    _tileFactory.DespawnTile(view);
            }

            _tileViews.Clear();
            _tileChunkRefs.Clear();
            _buildingOcclusionController.Reset();
        }

        void ApplyBlockingModeForView(in TileData tile, TileView view)
        {
            if (view == null || tile.identity.buildingId <= 0)
                return;

            if (!_buildingOcclusionController.IsBlocked(tile.identity.buildingId))
            {
                SetViewOcclusionState(view, false, _buildingOcclusionController.CurrentMode);
                return;
            }

            if (ResolveBuildingApplyMode(_buildingOcclusionController.CurrentMode) == BuildingApplyMode.FullDespawn)
                return;

            var applyMode = ResolveBuildingApplyMode(_buildingOcclusionController.CurrentMode);
            if (applyMode == BuildingApplyMode.AlphaBlend)
                SetBuildingViewsAlphaBlend(tile.identity.buildingId, true);
            else
                SetViewOcclusionState(view, true, _buildingOcclusionController.CurrentMode);
        }

        static void SetViewOcclusionState(TileView view, bool hidden, OcclusionMode mode)
        {
            if (view == null)
                return;

            var renderers = view.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = !hidden;

            if (mode != OcclusionMode.ColliderOnly)
                return;

            var colliders = view.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = true;
        }

        static BuildingApplyMode ResolveBuildingApplyMode(OcclusionMode mode) =>
            mode switch
            {
                OcclusionMode.RenderOnly => BuildingApplyMode.RenderOnly,
                OcclusionMode.ColliderOnly => BuildingApplyMode.ColliderOnly,
                OcclusionMode.AlphaBlendPreserve => BuildingApplyMode.AlphaBlend,
                _ => BuildingApplyMode.FullDespawn
            };

        enum BuildingApplyMode
        {
            FullDespawn = 0,
            RenderOnly = 1,
            ColliderOnly = 2,
            AlphaBlend = 3,
        }
    }
}
