// ============================================================
// BuildingPlayerOcclusionResolver — 카메라↔플레이어 시선상 가리는 buildingId 집합
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// 야외 전용. 카메라 지면점↔플레이어 월드 선분을 그리드로 샘플해 차단 타일의 buildingId를 수집합니다.
    /// </summary>
    public sealed class BuildingPlayerOcclusionResolver
    {
        static readonly Vector3Int[] CardinalDirs =
        {
            Vector3Int.right, Vector3Int.back, Vector3Int.left, Vector3Int.forward
        };

        readonly TileMapCacheHub _hub;
        readonly float _cellSize;
        readonly float _groundPlaneY;
        readonly Func<Camera> _resolveCamera;

        readonly List<(int x, int z)> _cellsOnSegment = new();
        readonly HashSet<(int x, int z)> _dedupeSegment = new();
        readonly HashSet<int> _blockingScratch = new();

        public BuildingPlayerOcclusionResolver(
            TileMapCacheHub hub,
            float cellSize,
            Func<Camera> resolveCamera,
            float groundPlaneY = 0f)
        {
            _hub = hub;
            _cellSize = cellSize > 0f ? cellSize : 1f;
            _resolveCamera = resolveCamera;
            _groundPlaneY = groundPlaneY;
        }

        public HashSet<int> ResolveBlockingBuildingIds(
            int floorBand,
            Vector3 playerWorld,
            int playerGridX,
            int playerGridZ)
        {
            _blockingScratch.Clear();

            Camera cam = _resolveCamera?.Invoke();
            if (cam == null)
                return _blockingScratch;

            Vector3 cameraGround = ResolveCameraGroundPoint(cam);
            CollectCellsOnViewSegment(cameraGround, playerWorld);
            CollectBlockingFromSegment(floorBand, playerGridX, playerGridZ);
            return _blockingScratch;
        }

        Vector3 ResolveCameraGroundPoint(Camera camera)
        {
            Vector3 origin = camera.transform.position;
            var ray = new Ray(origin, camera.transform.forward);
            if (new Plane(Vector3.up, new Vector3(0f, _groundPlaneY, 0f)).Raycast(ray, out float dist))
                return ray.GetPoint(dist);

            return new Vector3(origin.x, _groundPlaneY, origin.z);
        }

        void CollectCellsOnViewSegment(Vector3 fromWorld, Vector3 toWorld)
        {
            _cellsOnSegment.Clear();
            _dedupeSegment.Clear();

            float span = Vector3.Distance(fromWorld, toWorld);
            int steps = Mathf.Max(1, Mathf.CeilToInt(span / (_cellSize * 0.5f)));

            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0f : i / (float)steps;
                Vector3 p = Vector3.Lerp(fromWorld, toWorld, t);
                Vector3Int g = TileHelper.ConvertWorldToGrid(p, _cellSize);
                var cell = (g.x, g.z);
                if (_dedupeSegment.Add(cell))
                    _cellsOnSegment.Add(cell);
            }
        }

        void CollectBlockingFromSegment(int floorBand, int playerGridX, int playerGridZ)
        {
            (int x, int z) prev = default;
            bool hasPrev = false;

            foreach (var (x, z) in _cellsOnSegment)
            {
                if (x == playerGridX && z == playerGridZ)
                {
                    hasPrev = false;
                    continue;
                }

                TryAddBlockingAtCell(floorBand, x, z);

                if (hasPrev)
                    TryAddBlockingOnEdge(floorBand, prev.x, prev.z, x, z);

                prev = (x, z);
                hasPrev = true;
            }
        }

        void TryAddBlockingAtCell(int band, int x, int z)
        {
            if (!_hub.TryGetCellTiles(x, z, band, out var list))
                return;

            for (int i = 0; i < list.Count; i++)
            {
                if (!IsBlockingType((TileView.TileType)list[i].identity.tileType))
                    continue;

                int id = ResolveBuildingIdForBlocking(band, x, z, list[i]);
                if (id > 0)
                    _blockingScratch.Add(id);
            }
        }

        void TryAddBlockingOnEdge(int band, int ax, int az, int bx, int bz)
        {
            var cellA = new Vector3Int(ax, band, az);
            var cellB = new Vector3Int(bx, band, bz);

            if (_hub.TryGetEdgeBetween(cellA, cellB, out var edge) &&
                IsBlockingType((TileView.TileType)edge.identity.tileType))
            {
                int id = edge.identity.buildingId;
                if (id <= 0)
                    _hub.TryGetFloorBuildingRoom(band, ax, az, out id, out _);
                if (id > 0)
                    _blockingScratch.Add(id);
            }

            foreach (var d in CardinalDirs)
            {
                var n = new Vector3Int(ax, band, az) + d;
                if (n.x == bx && n.z == bz)
                    continue;

                if (_hub.TryGetEdgeBetween(new Vector3Int(ax, band, az), n, out edge) &&
                    IsBlockingType((TileView.TileType)edge.identity.tileType))
                {
                    int id = ResolveBuildingIdForBlocking(band, ax, az, edge);
                    if (id > 0)
                        _blockingScratch.Add(id);
                }
            }
        }

        static bool IsBlockingType(TileView.TileType type) =>
            type is TileView.TileType.Wall or TileView.TileType.EdgeWall or TileView.TileType.Obstacle;

        int ResolveBuildingIdForBlocking(int band, int x, int z, TileData blockingTile)
        {
            int id = blockingTile.identity.buildingId;
            if (id > 0)
                return id;

            return _hub.TryGetFloorBuildingRoom(band, x, z, out id, out _) ? id : 0;
        }
    }
}
