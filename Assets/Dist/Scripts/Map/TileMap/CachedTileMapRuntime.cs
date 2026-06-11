using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>공유 청크 인덱스 증분 갱신 래퍼.</summary>
    public class CachedTileMapRuntime : IMapModel
    {
        private readonly IMapModel _runtimeData;
        private readonly TileMapChunkIndex _chunkIndex;
        private readonly int _chunkSize;
        private readonly bool _ownsChunkIndex;

        public CachedTileMapRuntime(IMapModel runtimeData, TileMapChunkIndex sharedChunkIndex = null, int chunkSize = 16)
        {
            _runtimeData = runtimeData;
            _chunkSize = Mathf.Max(1, chunkSize);

            if (sharedChunkIndex != null)
            {
                _chunkIndex = sharedChunkIndex;
                _ownsChunkIndex = false;
            }
            else
            {
                _chunkIndex = new TileMapChunkIndex();
                _ownsChunkIndex = true;
                _chunkIndex.Build(runtimeData, _chunkSize);
            }

            if (_ownsChunkIndex)
            {
                _runtimeData.OnRuntimeTileAdded += HandleTileAdded;
                _runtimeData.OnRuntimeTileRemoved += HandleTileRemoved;
            }
        }

        public TileMapChunkIndex ChunkIndex => _chunkIndex;

        public ITileEdgeBinderReadOnly EdgeBinder => _runtimeData.EdgeBinder;

        public void GatherRenderableTiles(Vector3Int cellPos, List<TileData> buffer) =>
            _runtimeData.GatherRenderableTiles(cellPos, buffer);

        public IReadOnlyList<TileData> TilesSnapshot => _runtimeData.TilesSnapshot;

        public event Action<Vector3Int, IReadOnlyList<TileData>> OnRuntimeDataChanged
        {
            add => _runtimeData.OnRuntimeDataChanged += value;
            remove => _runtimeData.OnRuntimeDataChanged -= value;
        }

        public event Action<IReadOnlyCollection<Vector3Int>> OnRuntimeBatchChanged
        {
            add => _runtimeData.OnRuntimeBatchChanged += value;
            remove => _runtimeData.OnRuntimeBatchChanged -= value;
        }

        public event Action<TileData> OnRuntimeTileAdded
        {
            add => _runtimeData.OnRuntimeTileAdded += value;
            remove => _runtimeData.OnRuntimeTileAdded -= value;
        }

        public event Action<TileData> OnRuntimeTileRemoved
        {
            add => _runtimeData.OnRuntimeTileRemoved += value;
            remove => _runtimeData.OnRuntimeTileRemoved -= value;
        }

        public bool TryGetTiles(Vector3Int pos, out IReadOnlyList<TileData> tileList) =>
            _runtimeData.TryGetTiles(pos, out tileList);

        public bool TryGetCellTiles(int x, int z, int cellY, out IReadOnlyList<TileData> tileList) =>
            _runtimeData.TryGetCellTiles(x, z, cellY, out tileList);

        public IEnumerable<(int x, int z, int y)> EnumerateOccupiedCells() =>
            _runtimeData.EnumerateOccupiedCells();

        public bool TryGetTileById(Guid tileId, out TileData tileData) =>
            _runtimeData.TryGetTileById(tileId, out tileData);

        public void ForEachRuntimeTile(Action<TileData> visit) =>
            _runtimeData.ForEachRuntimeTile(visit);

        public void PatchTileIdentity(Guid tileDefId, int buildingId, int roomId) =>
            _runtimeData.PatchTileIdentity(tileDefId, buildingId, roomId);

        public void Initialize(MapModelDTO prepared)
        {
            _runtimeData.Initialize(prepared);
            if (_ownsChunkIndex)
                _chunkIndex.Build(_runtimeData, _chunkSize);
        }

        public void SetTile(TileData tileData) =>
            _runtimeData.SetTile(tileData);

        public void RemoveTile(TileData tileData) =>
            _runtimeData.RemoveTile(tileData);

        public void ApplyTiles(IReadOnlyList<TileData> tiles) =>
            _runtimeData.ApplyTiles(tiles);

        public void ApplyTileStates(IReadOnlyList<TileData> tiles) =>
            _runtimeData.ApplyTileStates(tiles);

        public void HideOcclusionTileWall(Vector3Int playerCellPos) =>
            _runtimeData.HideOcclusionTileWall(playerCellPos);

        public void UpdateOcclusionFromPlayerWorld(Vector3 playerWorld, OcclusionProximitySettings settings) =>
            _runtimeData.UpdateOcclusionFromPlayerWorld(playerWorld, settings);

        public void UpdateOcclusionFromPlayerWorld(
            Vector3 playerWorld,
            int playerFloorCellY,
            OcclusionProximitySettings settings) =>
            _runtimeData.UpdateOcclusionFromPlayerWorld(playerWorld, playerFloorCellY, settings);

        private void HandleTileAdded(TileData tileData) =>
            _chunkIndex.RegisterTile(tileData, _chunkSize);

        private void HandleTileRemoved(TileData tileData) =>
            _chunkIndex.UnregisterTile(tileData, _chunkSize);
    }
}
