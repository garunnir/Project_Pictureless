// ============================================================

// BuildingPlayerOcclusionResolver — 카메라↔플레이어 시선상 가리는 buildingId 집합

// ============================================================

using System;

using System.Collections.Generic;

using UnityEngine;



namespace IsoTilemap

{

    /// <summary>마지막 <see cref="BuildingPlayerOcclusionResolver.ResolveBlockingBuildingIds"/> 시선 샘플 스냅샷.</summary>

    public readonly struct SightLineBuildingDebugSnapshot

    {

        public bool IsValid { get; }

        public Vector3 CameraWorld { get; }

        public Vector3 PlayerWorld { get; }

        public IReadOnlyCollection<Vector3Int> SampledCells { get; }

        public IReadOnlyCollection<Vector3Int> BlockingCells { get; }

        public IReadOnlyCollection<int> BlockingBuildingIds { get; }



        public SightLineBuildingDebugSnapshot(

            bool isValid,

            Vector3 cameraWorld,

            Vector3 playerWorld,

            IReadOnlyCollection<Vector3Int> sampledCells,

            IReadOnlyCollection<Vector3Int> blockingCells,

            IReadOnlyCollection<int> blockingBuildingIds)

        {

            IsValid = isValid;

            CameraWorld = cameraWorld;

            PlayerWorld = playerWorld;

            SampledCells = sampledCells ?? Array.Empty<Vector3Int>();

            BlockingCells = blockingCells ?? Array.Empty<Vector3Int>();

            BlockingBuildingIds = blockingBuildingIds ?? Array.Empty<int>();

        }



        public static SightLineBuildingDebugSnapshot Empty =>

            new(false, Vector3.zero, Vector3.zero,

                Array.Empty<Vector3Int>(), Array.Empty<Vector3Int>(), Array.Empty<int>());

    }



    /// <summary>
    /// 카메라↔플레이어 3D 시선을 샘플해 경로상 그리드 셀 (x, y, z) 중
    /// 오클루전 플래그가 있는 타일의 buildingId를 수집합니다.
    /// <paramref name="excludeBuildingId"/>(플레이어 소속 building)는 제외합니다.
    /// </summary>

    public sealed class BuildingPlayerOcclusionResolver

    {

        readonly TileMapCacheHub _hub;

        readonly float _cellSize;

        readonly Func<Camera> _resolveCamera;



        readonly HashSet<Vector3Int> _cellsOnSegment = new();

        readonly HashSet<Vector3Int> _blockingCellsScratch = new();

        readonly HashSet<int> _blockingScratch = new();



        public BuildingPlayerOcclusionResolver(

            TileMapCacheHub hub,

            float cellSize,

            Func<Camera> resolveCamera)

        {

            _hub = hub;

            _cellSize = cellSize > 0f ? cellSize : 1f;

            _resolveCamera = resolveCamera;

        }



        public SightLineBuildingDebugSnapshot LastDebug { get; private set; } = SightLineBuildingDebugSnapshot.Empty;



        public bool TryGetCameraWorld(out Vector3 cameraWorld)

        {

            Camera cam = _resolveCamera?.Invoke();

            if (cam == null)

            {

                cameraWorld = Vector3.zero;

                return false;

            }



            cameraWorld = cam.transform.position;

            return true;

        }



        /// <summary>차단 후보 buildingId를 <paramref name="output"/>에 채웁니다.</summary>

        public void ResolveBlockingBuildingIds(

            Vector3 playerWorld,

            HashSet<int> output,

            int excludeBuildingId = 0)

        {

            output.Clear();

            _blockingCellsScratch.Clear();



            if (!TryGetCameraWorld(out Vector3 cameraWorld))

            {

                LastDebug = SightLineBuildingDebugSnapshot.Empty;

                return;

            }



            CollectBlockingOnSightSegment(cameraWorld, playerWorld, output, excludeBuildingId);

        }



        void CollectBlockingOnSightSegment(

            Vector3 cameraWorld,

            Vector3 playerWorld,

            HashSet<int> output,

            int excludeBuildingId)

        {

            _cellsOnSegment.Clear();



            float span = Vector3.Distance(cameraWorld, playerWorld);

            int steps = Mathf.Max(1, Mathf.CeilToInt(span / (_cellSize * 0.5f)));



            for (int i = 0; i <= steps; i++)

            {

                float t = steps == 0 ? 0f : i / (float)steps;

                Vector3 p = Vector3.Lerp(cameraWorld, playerWorld, t);

                Vector3Int cell = TileHelper.ConvertWorldToGrid(p, _cellSize);



                if (!_cellsOnSegment.Add(cell))

                    continue;



                if (AddBuildingIdsAtCell(cell, output, excludeBuildingId))

                    _blockingCellsScratch.Add(cell);

            }



            LastDebug = new SightLineBuildingDebugSnapshot(

                true,

                cameraWorld,

                playerWorld,

                _cellsOnSegment,

                _blockingCellsScratch,

                output);

        }



        bool AddBuildingIdsAtCell(Vector3Int cell, HashSet<int> output, int excludeBuildingId)

        {

            if (!_hub.TryGetCellTiles(cell.x, cell.z, cell.y, out var tiles))

                return false;



            bool contributed = false;

            for (int i = 0; i < tiles.Count; i++)

            {

                TileData tile = tiles[i];

                if (!TileOccludesSight(tile))

                    continue;



                int buildingId = tile.identity.buildingId;

                if (buildingId <= 0 || buildingId == excludeBuildingId)

                    continue;



                output.Add(buildingId);

                contributed = true;

            }



            return contributed;

        }



        static bool TileOccludesSight(in TileData tile) =>

            TileCollisionFlagsUtil.TileOccludesOccupiedCells(tile) ||

            TileCollisionFlagsUtil.TileOccludesEdge(tile);

    }

}


