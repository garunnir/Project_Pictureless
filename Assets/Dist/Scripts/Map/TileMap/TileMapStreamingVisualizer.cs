using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// 청크 단위 타일 스트리밍 뷰. 모델은 전체 유지, GameObject는 desired 청크만 스폰.
    /// 층 가시성은 despawn하지 않고 <see cref="TileViewPresentationApplier"/>가 처리합니다.
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
        private readonly List<Guid> _pruneGuids = new();

        private TileMapChunkIndex _chunkIndex;
        private IMapModelReadOnly _boundRuntime;
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

        public void SetPresentationApplier(TileViewPresentationApplier applier) => _presentationApplier = applier;

        public bool TryGetView(Guid tileId, out TileView view) => _tileViews.TryGetValue(tileId, out view);

        public void CollectSpawnedTileIds(List<Guid> into)
        {
            into.Clear();
            foreach (var kv in _tileViews)
                into.Add(kv.Key);
        }

        public void SyncFloorVisibility(in FloorVisibilityContext ctx) =>
            _presentationApplier?.SyncFloorVisibility(in ctx, _boundRuntime);

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

            _boundRuntime.GatherRenderableTiles(cellPos, _gatherBuffer);
            PruneDeletedTilesAtCell(cellPos, _gatherBuffer);
            RenderTiles(_gatherBuffer);
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

                RefreshCellAtLoadedChunk(cellPos);
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

                _boundRuntime.GatherRenderableTiles(cells[i], _gatherBuffer);
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
                return;
            }

            view.UpdateTile(tileData, CellSize);
            _presentationApplier?.SyncPresentationForTile(id);
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
                }
                else if (IsTileInLoadedChunk(tileData))
                {
                    AcquireTileInChunk(tileData, TileChunkCoord.FromCell(GetRepresentativeCell(tileData), _chunkSize));
                }
            }
        }

        /// <summary>모델에서 삭제된 타일만 제거합니다. 가시성 숨김과 무관합니다.</summary>
        private void PruneDeletedTilesAtCell(Vector3Int cell, List<TileData> gatheredAtCell)
        {
            if (_boundRuntime == null)
                return;

            _pruneGuids.Clear();
            foreach (var kv in _tileViews)
            {
                if (!ViewTouchesCell(kv.Value, cell))
                    continue;

                if (_boundRuntime.TryGetTileById(kv.Key, out _))
                    continue;

                _pruneGuids.Add(kv.Key);
            }

            for (int i = 0; i < _pruneGuids.Count; i++)
                DespawnViewCompletely(_pruneGuids[i]);
        }

        private static bool ViewTouchesCell(TileView view, Vector3Int cell)
        {
            if (view == null)
                return false;

            if (view.tileType == TileView.TileType.EdgeWall)
            {
                var key = new WallEdgeKey(view.gridPos, (WallFace)view.wallEdgeFace);
                return key.Anchor == cell || key.NeighborCell() == cell;
            }

            if (view.tileType == TileView.TileType.Floor)
            {
                var key = FloorFaceKey.ForWalkableCell(view.gridPos);
                return key.CellBelow == cell || key.CellAbove == cell;
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
            var type = (TileView.TileType)tileData.identity.tileType;
            if (type == TileView.TileType.Floor)
            {
                FloorFaceKey key = FloorFaceKey.FromFloorTileIdentity(tileData.identity);
                return IsCellInLoadedChunk(key.CellBelow) || IsCellInLoadedChunk(key.CellAbove);
            }

            if (type == TileView.TileType.EdgeWall)
            {
                WallEdgeKey key = WallEdgeKey.FromEdgeTileIdentity(tileData.identity);
                return IsCellInLoadedChunk(key.Anchor) || IsCellInLoadedChunk(key.NeighborCell());
            }

            if (_chunkIndex != null &&
                _chunkIndex.TryGetChunkForTile(tileData.tileDefId, out Vector2Int chunk))
                return _loadedChunks.Contains(chunk);

            return IsCellInLoadedChunk(GetRepresentativeCell(tileData));
        }

        private static Vector3Int GetRepresentativeCell(TileData tileData)
        {
            if ((TileView.TileType)tileData.identity.tileType == TileView.TileType.EdgeWall)
                return WallEdgeKey.FromEdgeTileIdentity(tileData.identity).Anchor;

            if ((TileView.TileType)tileData.identity.tileType == TileView.TileType.Floor)
                return FloorFaceKey.FromFloorTileIdentity(tileData.identity).CellBelow;

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
        }
    }
}
