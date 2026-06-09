// ============================================================
// BuildingPlayerOcclusionResolver — 카메라↔플레이어 시선상 가리는 buildingId 집합
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// 야외 전용. 카메라↔플레이어 3D 시선을 샘플해, 플레이어 셀을 제외한
    /// 경로상 그리드 셀 (x, y, z) 타일의 buildingId만 수집합니다.
    /// </summary>
    public sealed class BuildingPlayerOcclusionResolver
    {
        readonly TileMapCacheHub _hub;
        readonly float _cellSize;
        readonly Func<Camera> _resolveCamera;

        readonly HashSet<Vector3Int> _cellsOnSegment = new();
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
            _ = groundPlaneY;
        }

        public HashSet<int> ResolveBlockingBuildingIds(
            Vector3 playerWorld,
            Vector3Int playerCell)
        {
            _blockingScratch.Clear();

            Camera cam = _resolveCamera?.Invoke();
            if (cam == null)
                return _blockingScratch;

            CollectBlockingOnSightSegment(cam.transform.position, playerWorld, playerCell);
            return _blockingScratch;
        }

        void CollectBlockingOnSightSegment(
            Vector3 cameraWorld,
            Vector3 playerWorld,
            Vector3Int playerCell)
        {
            _cellsOnSegment.Clear();

            float span = Vector3.Distance(cameraWorld, playerWorld);
            int steps = Mathf.Max(1, Mathf.CeilToInt(span / (_cellSize * 0.5f)));

            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0f : i / (float)steps;
                Vector3 p = Vector3.Lerp(cameraWorld, playerWorld, t);
                Vector3Int cell = TileHelper.ConvertWorldToGrid(p, _cellSize);

                if (cell == playerCell)
                    continue;

                if (!_cellsOnSegment.Add(cell))
                    continue;

                AddBuildingIdsAtCell(cell);
            }
        }

        void AddBuildingIdsAtCell(Vector3Int cell)
        {
            if (!_hub.TryGetCellTiles(cell.x, cell.z, cell.y, out var tiles))
                return;

            for (int i = 0; i < tiles.Count; i++)
            {
                int buildingId = tiles[i].identity.buildingId;
                if (buildingId > 0)
                    _blockingScratch.Add(buildingId);
            }
        }
    }
}
