using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public class TileMapModel : IMapModel
    {
        public event Action<Vector3Int, IReadOnlyList<TileData>> OnRuntimeDataChanged;
        public event Action<IReadOnlyCollection<Vector3Int>> OnRuntimeBatchChanged;
        public event Action<TileData> OnRuntimeTileAdded;
        public event Action<TileData> OnRuntimeTileRemoved;

        public Dictionary<Vector3Int, List<TileData>> tiles = new Dictionary<Vector3Int, List<TileData>>();

        private readonly Dictionary<Guid, TileData> _tilesById = new Dictionary<Guid, TileData>();
        private readonly TileEdgeBinder _edgeBinder = new TileEdgeBinder();

        private List<TileData> _cachedList = new List<TileData>();
        private bool _isDirty;
        private WallOcclusionFinder _occlusionFinder;
        private readonly HashSet<Guid> _hiddenWallTileIds = new HashSet<Guid>();
        private readonly Dictionary<Guid, TileData> _hiddenWallTileCache = new Dictionary<Guid, TileData>();
        private readonly List<TileData> _occlusionApplyBuffer = new List<TileData>();
        private readonly Dictionary<Guid, float> _lastAppliedOcclusion = new Dictionary<Guid, float>();
        private readonly List<OcclusionWallEntry> _occlusionWallEntries = new List<OcclusionWallEntry>();
        private readonly HashSet<Vector3Int> _changedCellsBuffer = new HashSet<Vector3Int>();

        private bool _hasLastOcclusionPlayerCell;
        private Vector3Int _lastOcclusionPlayerCell;
        private FloorRoomCache _floorRoomCache;

        public ITileEdgeBinderReadOnly EdgeBinder => _edgeBinder;

        public void SetFloorRoomCache(FloorRoomCache cache) => _floorRoomCache = cache;

        public void GatherRenderableTiles(Vector3Int cellPos, List<TileData> buffer)
        {
            buffer.Clear();

            if (tiles.TryGetValue(cellPos, out var list))
                buffer.AddRange(list);

            _edgeBinder.AppendIncidentEdges(cellPos, buffer);
        }

        private void NotifyCell(Vector3Int cell)
        {
            var snapshot = new List<TileData>();
            GatherRenderableTiles(cell, snapshot);
            OnRuntimeDataChanged?.Invoke(cell, snapshot);
        }

        public void AddTile(Vector3Int pos, TileData tile) => SetTile(tile);

        public IReadOnlyList<TileData> TilesSnapshot
        {
            get
            {
                if (_isDirty)
                {
                    _cachedList.Clear();
                    foreach (var list in tiles.Values)
                        _cachedList.AddRange(list);

                    foreach (var edgeTile in _edgeBinder.EnumerateTiles())
                        _cachedList.Add(edgeTile);

                    _isDirty = false;
                }

                return _cachedList;
            }
        }

        public void SetTile(TileData tileData)
        {
            if ((TileView.TileType)tileData.identity.tileType == TileView.TileType.EdgeWall)
                SetEdgeTile(tileData);
            else
                SetCellTile(tileData);

            InvalidateOcclusionPlayerTracking();
            InvalidateFloorRoomGeometry();
        }

        public void RemoveTile(TileData tileData)
        {
            var changedCells = new HashSet<Vector3Int>();
            bool removed = false;

            if ((TileView.TileType)tileData.identity.tileType == TileView.TileType.EdgeWall)
            {
                if (_edgeBinder.TryRemove(tileData.tileDefId, out var removedTile))
                {
                    removed = true;
                    tileData = removedTile;
                    var key = WallEdgeKey.FromEdgeTileIdentity(tileData.identity);
                    changedCells.Add(key.Anchor);
                    changedCells.Add(key.NeighborCell());
                }
            }
            else
            {
                Vector3Int pos = tileData.identity.GridPos;
                if (tiles.TryGetValue(pos, out var list))
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if (list[i].tileDefId != tileData.tileDefId)
                            continue;

                        tileData = list[i];
                        list.RemoveAt(i);
                        removed = true;
                        changedCells.Add(pos);
                        break;
                    }

                    if (list.Count == 0)
                        tiles.Remove(pos);
                }
            }

            if (!removed)
                return;

            _tilesById.Remove(tileData.tileDefId);
            _isDirty = true;
            InvalidateOcclusionPlayerTracking();
            InvalidateFloorRoomGeometry();
            OnRuntimeTileRemoved?.Invoke(tileData);

            foreach (var cell in changedCells)
                NotifyCell(cell);
        }

        public bool TryGetTileById(Guid tileId, out TileData tileData) => TryFindTileById(tileId, out tileData);

        private void SetEdgeTile(TileData tileData)
        {
            var key = WallEdgeKey.FromEdgeTileIdentity(tileData.identity);
            if (_edgeBinder.TryGetTile(key, out var previous))
            {
                OnRuntimeTileRemoved?.Invoke(previous);
                _tilesById.Remove(previous.tileDefId);
            }

            _edgeBinder.Register(tileData);
            IndexTile(tileData);
            _isDirty = true;
            OnRuntimeTileAdded?.Invoke(tileData);

            NotifyCell(key.Anchor);
            NotifyCell(key.NeighborCell());
        }

        private void SetCellTile(TileData tileData)
        {
            Vector3Int pos = tileData.identity.GridPos;
            if (!tiles.TryGetValue(pos, out var list))
            {
                tiles[pos] = new List<TileData> { tileData };
                IndexTile(tileData);
                _isDirty = true;
                OnRuntimeTileAdded?.Invoke(tileData);
                NotifyCell(pos);
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].tileDefId != tileData.tileDefId)
                    continue;

                list[i] = tileData;
                IndexTile(tileData);
                _isDirty = true;
                NotifyCell(pos);
                return;
            }

            list.Add(tileData);
            IndexTile(tileData);
            _isDirty = true;
            OnRuntimeTileAdded?.Invoke(tileData);
            NotifyCell(pos);
        }

        public void Initialize(MapModelDTO prepared)
        {
            tiles.Clear();
            _edgeBinder.Clear();
            _tilesById.Clear();

            foreach (var kv in prepared.TilesData)
            {
                if ((TileView.TileType)kv.identity.tileType == TileView.TileType.EdgeWall)
                {
                    _edgeBinder.Register(kv);
                    IndexTile(kv);
                }
                else
                {
                    if (!tiles.ContainsKey(kv.identity.GridPos))
                        tiles[kv.identity.GridPos] = new List<TileData>();

                    tiles[kv.identity.GridPos].Add(kv);
                    IndexTile(kv);
                }
            }

            _isDirty = true;
            _occlusionFinder = new WallOcclusionFinder(tiles, _edgeBinder.EdgeIndex);
            _hiddenWallTileIds.Clear();
            _hiddenWallTileCache.Clear();
            _occlusionWallEntries.Clear();
            _lastAppliedOcclusion.Clear();
            _hasLastOcclusionPlayerCell = false;
            InvalidateFloorRoomGeometry();
        }

        public IReadOnlyList<TileData> GetOccludingWalls(Vector3Int playerCellPos)
        {
            _occlusionFinder ??= new WallOcclusionFinder(tiles, _edgeBinder.EdgeIndex);
            return _occlusionFinder.Find(playerCellPos);
        }

        public bool TryGetTiles(Vector3Int pos, out IReadOnlyList<TileData> tileList)
        {
            if (tiles.TryGetValue(pos, out var list))
            {
                tileList = list;
                return true;
            }

            tileList = null;
            return false;
        }

        /// <summary>BFS 결과 집합만 갱신하고 거리 occlusion을 채운 뒌 API(호환용). 월드 기반 갱신은 <see cref="UpdateOcclusionFromPlayerWorld"/>를 쓰세요.</summary>
        public void HideOcclusionTileWall(Vector3Int playerCellPos)
        {
            var settings = OcclusionProximitySettings.DefaultUnity;
            Vector3 world = TileHelper.ConvertGridToWorldPos(playerCellPos, settings.CellSize);
            UpdateOcclusionFromPlayerWorld(world, settings);
        }

        /// <summary>플레이어 월드 위치만으로 셀 전이 시 BFS + 매 호출마다 숨김 집합에 대한 거리 occlusion 갱신.</summary>
        public void UpdateOcclusionFromPlayerWorld(Vector3 playerWorld, OcclusionProximitySettings settings)
        {
            float cs = Mathf.Max(1e-4f, settings.CellSize);

            NormalizeProximity(ref settings);

            Vector3Int cell = TileHelper.ConvertWorldToGrid(playerWorld, cs);

            bool needRebuild = !_hasLastOcclusionPlayerCell || cell != _lastOcclusionPlayerCell;
            if (needRebuild)
            {
                RebuildOcclusionMembership(cell, playerWorld, settings);
                _hasLastOcclusionPlayerCell = true;
                _lastOcclusionPlayerCell = cell;
            }

            RefreshOcclusionProximity(playerWorld, settings);
        }

        private static void NormalizeProximity(ref OcclusionProximitySettings s)
        {
            if (s.OcclusionFullWithinDistance > s.OcclusionNoneBeyondDistance)
            {
                (s.OcclusionFullWithinDistance, s.OcclusionNoneBeyondDistance) =
                    (s.OcclusionNoneBeyondDistance, s.OcclusionFullWithinDistance);
            }

            float minSpan = 1e-3f;
            if (Mathf.Abs(s.OcclusionNoneBeyondDistance - s.OcclusionFullWithinDistance) < minSpan)
                s.OcclusionNoneBeyondDistance = s.OcclusionFullWithinDistance + minSpan;

            if (s.ApplyEpsilon < 0f)
                s.ApplyEpsilon = 0f;
        }

        private void InvalidateOcclusionPlayerTracking() =>
            _hasLastOcclusionPlayerCell = false;

        private void InvalidateFloorRoomGeometry() =>
            _floorRoomCache?.InvalidateAll();

        private void RebuildOcclusionMembership(Vector3Int playerCellPos, Vector3 playerWorld,
            OcclusionProximitySettings settings)
        {
            HashSet<(int x, int z)> roomVisited = null;
            if (_floorRoomCache != null)
            {
                roomVisited = _floorRoomCache.GetOrComputeVisited(
                    playerCellPos.y, playerCellPos.x, playerCellPos.z,
                    FloorRoomBfsProfile.Occlusion);
            }

            _occlusionFinder ??= new WallOcclusionFinder(tiles, _edgeBinder.EdgeIndex);
            OcclusionSelection batch = _occlusionFinder.FindOcclusion(playerCellPos, roomVisited);
            var currentHiddenIds = new HashSet<Guid>();
            _occlusionApplyBuffer.Clear();

            var list = batch.FinalOccluding;
            float cs = Mathf.Max(1e-4f, settings.CellSize);

            for (int i = 0; i < list.Count; i++)
            {
                TileData wall = list[i];
                currentHiddenIds.Add(wall.tileDefId);

                TileIdentity wi = wall.identity;
                float occ = ComputeOcclusionStrength(playerWorld, wi, cs, settings);
                TileState state = wall.state;
                state.characterOcclusion = occ;
                wall.state = state;
                _occlusionApplyBuffer.Add(wall);
                _hiddenWallTileCache[wall.tileDefId] = wall;
                _lastAppliedOcclusion[wall.tileDefId] = occ;
            }

            foreach (Guid hiddenId in _hiddenWallTileIds)
            {
                if (currentHiddenIds.Contains(hiddenId))
                    continue;

                if (!_hiddenWallTileCache.TryGetValue(hiddenId, out var hiddenTile) &&
                    !_tilesById.TryGetValue(hiddenId, out hiddenTile))
                {
                    continue;
                }

                TileState state = hiddenTile.state;
                if (state.characterOcclusion > 0f)
                {
                    state.characterOcclusion = 0f;
                    hiddenTile.state = state;
                    _occlusionApplyBuffer.Add(hiddenTile);
                }

                _hiddenWallTileCache.Remove(hiddenId);
                _lastAppliedOcclusion.Remove(hiddenId);
            }

            _hiddenWallTileIds.Clear();
            foreach (Guid id in currentHiddenIds)
                _hiddenWallTileIds.Add(id);

            RebuildOcclusionWallEntries(cs);

            if (_occlusionApplyBuffer.Count > 0)
                ApplyTiles(_occlusionApplyBuffer);
        }

        private void RebuildOcclusionWallEntries(float cellSize)
        {
            _occlusionWallEntries.Clear();
            cellSize = Mathf.Max(1e-4f, cellSize);

            foreach (Guid id in _hiddenWallTileIds)
            {
                if (!_hiddenWallTileCache.TryGetValue(id, out TileData wall) &&
                    !_tilesById.TryGetValue(id, out wall))
                    continue;

                Vector3 wallPoint = OcclusionWallWorldPoint(wall.identity, cellSize);
                _occlusionWallEntries.Add(new OcclusionWallEntry(id, wallPoint.x, wallPoint.z));
            }
        }

        private void RefreshOcclusionProximity(Vector3 playerWorld, OcclusionProximitySettings settings)
        {
            if (_occlusionWallEntries.Count == 0)
                return;

            float cs = Mathf.Max(1e-4f, settings.CellSize);
            float eps = settings.ApplyEpsilon;
            _occlusionApplyBuffer.Clear();

            for (int i = 0; i < _occlusionWallEntries.Count; i++)
            {
                OcclusionWallEntry entry = _occlusionWallEntries[i];
                Guid id = entry.TileId;

                if (!_hiddenWallTileCache.TryGetValue(id, out TileData wall))
                {
                    if (!_tilesById.TryGetValue(id, out wall))
                        continue;

                    _hiddenWallTileCache[id] = wall;
                }

                float d = Mathf.Sqrt(OcclusionDistSqXZ(playerWorld, entry.WallWorldX, entry.WallWorldZ));
                float occ = OcclusionCurve(d, settings);

                if (_lastAppliedOcclusion.TryGetValue(id, out float prev) &&
                    Mathf.Abs(occ - prev) <= eps)
                    continue;

                TileState state = wall.state;
                state.characterOcclusion = occ;
                wall.state = state;
                _occlusionApplyBuffer.Add(wall);
                _hiddenWallTileCache[id] = wall;
                _lastAppliedOcclusion[id] = occ;
            }

            if (_occlusionApplyBuffer.Count > 0)
                ApplyTiles(_occlusionApplyBuffer);
        }

        private static float ComputeOcclusionStrength(
            Vector3 playerWorld,
            TileIdentity identity,
            float cellSize,
            OcclusionProximitySettings settings)
        {
            Vector3 wallPoint = OcclusionWallWorldPoint(identity, cellSize);
            float d = Mathf.Sqrt(OcclusionDistSqXZ(playerWorld, wallPoint));
            return OcclusionCurve(d, settings);
        }

        private static float OcclusionDistSqXZ(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private static float OcclusionDistSqXZ(Vector3 a, float wallX, float wallZ)
        {
            float dx = a.x - wallX;
            float dz = a.z - wallZ;
            return dx * dx + dz * dz;
        }

        private static Vector3 OcclusionWallWorldPoint(TileIdentity identity, float cellSize)
        {
            var type = (TileView.TileType)identity.tileType;
            if (type == TileView.TileType.EdgeWall && identity.edgeFace != TileIdentity.EdgeFaceNone)
            {
                WallEdgeKey key = WallEdgeKey.FromEdgeTileIdentity(identity);
                WallEdgeKey.GetWorldPose(key, cellSize, out Vector3 pose, out _);
                return pose;
            }

            Vector3 sizeF = (Vector3)identity.sizeUnit;
            Vector3 centroidOffset = (sizeF - Vector3.one) * 0.5f;
            Vector3 gridCenter = (Vector3)identity.GridPos + centroidOffset;
            return TileHelper.ConvertGridToWorldPos(gridCenter, cellSize);
        }

        private static float OcclusionCurve(float distance, OcclusionProximitySettings s)
        {
            float clamped =
                Mathf.Clamp(distance, s.OcclusionFullWithinDistance, s.OcclusionNoneBeyondDistance);
            return Mathf.InverseLerp(
                s.OcclusionNoneBeyondDistance,
                s.OcclusionFullWithinDistance,
                clamped);
        }

        private bool TryFindTileById(Guid tileId, out TileData tileData) =>
            _tilesById.TryGetValue(tileId, out tileData);

        private static void IndexTile(in TileData tile, Dictionary<Guid, TileData> index) =>
            index[tile.tileDefId] = tile;

        private void IndexTile(in TileData tile) => IndexTile(tile, _tilesById);

        public void ApplyTiles(IReadOnlyList<TileData> tileList)
        {
            _changedCellsBuffer.Clear();

            foreach (var tile in tileList)
            {
                if ((TileView.TileType)tile.identity.tileType == TileView.TileType.EdgeWall)
                {
                    if (!_edgeBinder.TryReplaceTileData(tile))
                        continue;

                    IndexTile(tile);
                    var key = WallEdgeKey.FromEdgeTileIdentity(tile.identity);
                    _changedCellsBuffer.Add(key.Anchor);
                    _changedCellsBuffer.Add(key.NeighborCell());
                    continue;
                }

                var pos = tile.identity.GridPos;

                if (!tiles.TryGetValue(pos, out var existingList))
                    continue;

                for (int i = 0; i < existingList.Count; i++)
                {
                    if (existingList[i].tileDefId != tile.tileDefId)
                        continue;

                    existingList[i] = tile;
                    IndexTile(tile);
                    break;
                }

                _changedCellsBuffer.Add(pos);
            }

            _isDirty = true;

            if (_changedCellsBuffer.Count > 0)
                OnRuntimeBatchChanged?.Invoke(_changedCellsBuffer);
        }

        private readonly struct OcclusionWallEntry
        {
            public readonly Guid TileId;
            public readonly float WallWorldX;
            public readonly float WallWorldZ;

            public OcclusionWallEntry(Guid tileId, float wallWorldX, float wallWorldZ)
            {
                TileId = tileId;
                WallWorldX = wallWorldX;
                WallWorldZ = wallWorldZ;
            }
        }
    }
}
