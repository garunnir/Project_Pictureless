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
            CollectBuildingIdsFromSampledCells(playerGridX, playerGridZ);
            return _blockingScratch;
        }

        Vector3 ResolveCameraGroundPoint(Camera camera)
        {
            Vector3 origin = camera.transform.position;
            // NOTE:
            // forward-레이의 지면 교차점은 카메라 시선 중심점(look-at)에 가까워
            // 실제 "관측자 위치"보다 앞쪽 셀을 기준으로 삼게 됩니다.
            // 그 결과 앞/뒤 판정이 뒤집히는 케이스가 발생하므로
            // 카메라 월드 위치를 지면(Y)으로 투영한 점을 기준으로 사용합니다.
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

        void CollectBuildingIdsFromSampledCells(int playerGridX, int playerGridZ)
        {
            foreach (var (x, z) in _cellsOnSegment)
            {
                if (x == playerGridX && z == playerGridZ)
                    continue;

                AddBuildingIdsAtCell(x, z);
            }
        }

        void AddBuildingIdsAtCell(int x, int z)
        {
            foreach (var occupied in _hub.EnumerateOccupiedCells())
            {
                if (occupied.x != x || occupied.z != z)
                    continue;

                if (!_hub.TryGetCellTiles(x, z, occupied.band, out var tiles))
                    continue;

                for (int i = 0; i < tiles.Count; i++)
                {
                    int buildingId = tiles[i].identity.buildingId;
                    if (buildingId > 0)
                        _blockingScratch.Add(buildingId);
                }
            }
        }
    }
}
