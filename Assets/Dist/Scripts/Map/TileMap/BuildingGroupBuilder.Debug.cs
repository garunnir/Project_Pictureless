// ============================================================
// BuildingGroupBuilder.Debug — bake 디버그 로그·진단
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public sealed partial class BuildingGroupBuilder
    {
        void LogBakeSummaryIfDebug()
        {
            if (!Config.DebugMode.FloorAlgorithm)
                return;

            int faceCount = 0;
            int bakedAboveMin = 0;
            int outdoorMin = 0;
            foreach (var tile in _model.TilesSnapshot)
            {
                if (!TileIdentityUtil.IsFloorTile(tile.identity))
                    continue;

                faceCount++;
                int walkY = FloorFaceKey.FromFloorTileIdentity(tile.identity).CellAbove.y;
                int bid = tile.identity.buildingId;
                if (walkY == _minCellY && bid == TileIdentity.BuildingIdOutdoor)
                    outdoorMin++;
                else if (walkY > _minCellY && BuildingIdBakeRules.CanPropagateBuildingIdFrom(bid))
                    bakedAboveMin++;
            }

            Debug.Log(
                $"[BuildingGroupBuilder] bake: minCellY={_minCellY}, floorFaces={faceCount}, " +
                $"outdoor@min={outdoorMin}, upperWithBuildingId={bakedAboveMin}, buildings={_registry.TilesByBuildingId.Count}");

            foreach (var kv in _registry.TilesByBuildingId)
            {
                if (!_registry.TryGetBuildingExtent(kv.Key, out var extent) || !extent.HasBounds)
                    continue;

                int sliceCount = extent.FloorFootprintByCellY.Count;
                Debug.Log(
                    $"[BuildingGroupBuilder] extent id={extent.BuildingId} " +
                    $"aabb=({extent.MinX},{extent.MinOccupiedY},{extent.MinZ})-({extent.MaxX},{extent.MaxOccupiedY},{extent.MaxZ}) " +
                    $"maxStructuralY={extent.MaxStructuralY} floorSlices={sliceCount}");
                break;
            }

            int spaceCount = 0;
            int outdoorSpaces = 0;
            foreach (int spaceId in _hub.Spaces.Registry.SpaceIds)
            {
                spaceCount++;
                if (_hub.Spaces.IsOutdoorSpace(spaceId))
                    outdoorSpaces++;
            }

            if (spaceCount > 0)
            {
                Debug.Log(
                    $"[BuildingGroupBuilder] spaces={spaceCount} outdoor={outdoorSpaces} indoor={spaceCount - outdoorSpaces}");
                LogSpaceLeakDiagnosis();
            }

            int shellTagged = 0;
            int shellUntagged = 0;
            int shellTaggedMaxY = int.MinValue;
            foreach (var tile in _model.TilesSnapshot)
            {
                if (!TileIdentityUtil.IsStructural(tile.identity))
                    continue;

                int bid = tile.identity.buildingId;
                if (BuildingIdBakeRules.CanPropagateBuildingIdFrom(bid))
                {
                    shellTagged++;
                    int y = OccupiedCellCoord.PrimaryCellFromIdentity(tile.identity).y;
                    if (y > shellTaggedMaxY)
                        shellTaggedMaxY = y;
                }
                else
                {
                    shellUntagged++;
                }
            }

            Debug.Log(
                $"[BuildingGroupBuilder] structuralShell flood visited={_lastStructuralFloodVisited} " +
                $"patched={_lastStructuralFloodPatched} bridgedFloors={_lastStructuralFloodBridgedFloors} | " +
                $"afterBake tagged={shellTagged} untagged={shellUntagged} taggedMaxY={shellTaggedMaxY}");

            LogBuildingConnectionDiagnosis(3, 24);
        }

        void LogSpaceLeakDiagnosis()
        {
            var registry = _hub.Spaces.Registry;
            var index = _topology.Index;
            int ceilingOnly = 0;
            int lateralOnly = 0;
            int both = 0;
            int indoor = 0;

            foreach (int spaceId in registry.SpaceIds)
            {
                if (!registry.TryGetSpace(spaceId, out var space))
                    continue;

                if (!_registry.TryGetBuildingExtent(space.BuildingId, out var extent))
                    continue;

                var cells = registry.GetFloorCells(spaceId);
                SpaceLeakEvaluator.EvaluateComponents(
                    cells, space.BuildingId, extent, index, out bool ceiling, out bool lateral);

                if (!ceiling && !lateral)
                    indoor++;
                else if (ceiling && lateral)
                    both++;
                else if (ceiling)
                    ceilingOnly++;
                else
                    lateralOnly++;
            }

            Debug.Log(
                $"[BuildingGroupBuilder] spaceLeak: indoor={indoor} ceilingOnly={ceilingOnly} " +
                $"lateralOnly={lateralOnly} both={both}");
        }

        void LogBuildingConnectionDiagnosis(int idA, int idB)
        {
            if (!Config.DebugMode.FloorAlgorithm)
                return;

            if (!_registry.TilesByBuildingId.ContainsKey(idA) ||
                !_registry.TilesByBuildingId.ContainsKey(idB))
            {
                Debug.Log(
                    $"[BuildingDiagnosis] id={idA}∨{idB} 없음. bakedBuildingCount={_registry.TilesByBuildingId.Count}");
                return;
            }

            if (!_registry.TryGetBuildingExtent(idA, out var extA) || !extA.HasBounds ||
                !_registry.TryGetBuildingExtent(idB, out var extB) || !extB.HasBounds)
            {
                Debug.Log($"[BuildingDiagnosis] id={idA} or {idB} extent 없음.");
                return;
            }

            Debug.Log(
                $"[BuildingDiagnosis] id={idA} aabb=({extA.MinX},{extA.MinOccupiedY},{extA.MinZ})-" +
                $"({extA.MaxX},{extA.MaxOccupiedY},{extA.MaxZ}) maxStructuralY={extA.MaxStructuralY}");
            Debug.Log(
                $"[BuildingDiagnosis] id={idB} aabb=({extB.MinX},{extB.MinOccupiedY},{extB.MinZ})-" +
                $"({extB.MaxX},{extB.MaxOccupiedY},{extB.MaxZ}) maxStructuralY={extB.MaxStructuralY}");

            int cardinalFloorTouches = 0;
            int nearestFloorChebyshev = int.MaxValue;
            (int x, int z, int y) nearestA = default;
            (int x, int z, int y) nearestB = default;

            for (int cellY = _minCellY; cellY <= _maxCellY; cellY++)
            {
                if (!extA.TryGetFloorFootprint(cellY, out var footA) ||
                    !extB.TryGetFloorFootprint(cellY, out var footB))
                    continue;

                foreach (var (ax, az) in footA)
                {
                    foreach (var d in CardinalDirs)
                    {
                        int nx = ax + d.x;
                        int nz = az + d.z;
                        if (!FootprintContains(footB, nx, nz))
                            continue;

                        cardinalFloorTouches++;
                        int bidA = GetFloorBuildingId(ax, cellY, az);
                        int bidB = GetFloorBuildingId(nx, cellY, nz);
                        Debug.Log(
                            $"[BuildingDiagnosis] sliceY={cellY} cardinal floor touch A=({ax},{cellY},{az}) bid={bidA} " +
                            $"B=({nx},{cellY},{nz}) bid={bidB}");
                    }

                    foreach (var (bx, bz) in footB)
                    {
                        int cheb = Math.Max(Math.Abs(ax - bx), Math.Abs(az - bz));
                        if (cheb >= nearestFloorChebyshev)
                            continue;

                        nearestFloorChebyshev = cheb;
                        nearestA = (ax, az, cellY);
                        nearestB = (bx, bz, cellY);
                    }
                }
            }

            if (cardinalFloorTouches == 0)
            {
                Debug.Log(
                    $"[BuildingDiagnosis] 같은 slice에 cardinal 인접 floor **없음** → MergeBuildingsOnFloorAdjacency 대상 아님. " +
                    $"최근 floor chebyshev={nearestFloorChebyshev} A=({nearestA.x},{nearestA.y},{nearestA.z}) B=({nearestB.x},{nearestB.y},{nearestB.z})");
            }

            int shellBetweenWrongId = 0;
            int shellBetweenUntagged = 0;
            if (nearestFloorChebyshev < int.MaxValue && nearestFloorChebyshev <= 3)
            {
                int minX = Math.Min(nearestA.x, nearestB.x) - 1;
                int maxX = Math.Max(nearestA.x, nearestB.x) + 1;
                int minZ = Math.Min(nearestA.z, nearestB.z) - 1;
                int maxZ = Math.Max(nearestA.z, nearestB.z) + 1;
                int minY = Math.Min(nearestA.y, nearestB.y);
                int maxY = Math.Max(nearestA.y, nearestB.y) + 2;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        for (int z = minZ; z <= maxZ; z++)
                        {
                            if (!_topology.TryCollectTilesAtOccupiedCell(x, y, z, _occupiedCellCollectScratch))
                                continue;

                            for (int i = 0; i < _occupiedCellCollectScratch.Count; i++)
                            {
                                TileData tile = _occupiedCellCollectScratch[i];
                                if (!BuildingIdBakeRules.ShouldPatchBuildingIdAtOccupiedCell(tile.identity))
                                    continue;

                                int bid = tile.identity.buildingId;
                                if (bid == 0)
                                    shellBetweenUntagged++;
                                else if (bid != idA && bid != idB)
                                    shellBetweenWrongId++;
                            }
                        }
                    }
                }

                Debug.Log(
                    $"[BuildingDiagnosis] nearest gap box shell untagged={shellBetweenUntagged} otherBuildingId={shellBetweenWrongId} " +
                    $"mismatchShell3={CountShellWithBuildingIdInBox(minX, maxX, minY, maxY, minZ, maxZ, idA)} " +
                    $"mismatchShell24={CountShellWithBuildingIdInBox(minX, maxX, minY, maxY, minZ, maxZ, idB)}");
            }
        }

        int CountShellWithBuildingIdInBox(
            int minX, int maxX, int minY, int maxY, int minZ, int maxZ, int buildingId)
        {
            int count = 0;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        if (!_topology.TryCollectTilesAtOccupiedCell(x, y, z, _occupiedCellCollectScratch))
                            continue;

                        for (int i = 0; i < _occupiedCellCollectScratch.Count; i++)
                        {
                            TileData tile = _occupiedCellCollectScratch[i];
                            if (!BuildingIdBakeRules.ShouldPatchBuildingIdAtOccupiedCell(tile.identity))
                                continue;

                            if (tile.identity.buildingId == buildingId)
                                count++;
                        }
                    }
                }
            }

            return count;
        }

        static bool FootprintContains(IReadOnlyCollection<(int x, int z)> foot, int x, int z)
        {
            foreach (var (fx, fz) in foot)
            {
                if (fx == x && fz == z)
                    return true;
            }

            return false;
        }

        void VerifyOccupancyIndexAfterBake()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Config.DebugMode.FloorAlgorithm)
                return;

            foreach (var tile in _model.TilesSnapshot)
            {
                if (!TileIdentityUtil.IsStructural(tile.identity))
                    continue;

                Vector3Int primary = OccupiedCellCoord.PrimaryCellFromIdentity(tile.identity);
                if (_topology.Index.HasAnyTile(primary.x, primary.z, primary.y))
                    continue;

                Debug.LogWarning(
                    $"[BuildingGroupBuilder] 점유 인덱스 미등록: prefab={tile.identity.PrefabId} primary={primary}");
            }
#endif
        }
    }
}
