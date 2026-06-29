// ============================================================
// BuildingGroupBuilder.SpaceBake — Space flood·isOutdoor bake
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public sealed partial class BuildingGroupBuilder
    {
        void BakeAllSpaces()
        {
            var registry = _hub.Spaces.Registry;
            registry.Clear();
            var index = _topology.Index;

            _roomKeyScratch.Clear();
            _hub.Rooms.CollectRoomKeys(FloorRoomBfsProfile.Occlusion, _roomKeyScratch);
            _roomKeyScratch.Sort(CompareRoomKeys);

            foreach (var roomKey in _roomKeyScratch)
            {
                if (!_hub.Rooms.TryGet(roomKey, FloorRoomBfsProfile.Occlusion, out var occlusion) ||
                    occlusion.Visited == null)
                    continue;

                foreach (var (x, z) in occlusion.Visited)
                {
                    var cell = new Vector3Int(x, roomKey.CellY, z);
                    if (registry.TryGetSpaceAtFloorCell(cell, out _))
                        continue;

                    if (roomKey.BuildingId == 3)
                    {
                        Debug.Log(
                            $"[SpaceBake] floodSeed buildingId=3 roomY={roomKey.CellY} roomId={roomKey.RoomId} " +
                            $"startCell={cell}");
                    }

                    SpaceFloodResult flood = SpaceFloodFill3D.Run(
                        index, registry, cell, roomKey.BuildingId);

                    if (flood.VisitedFloor.Count == 0)
                        continue;

                    if (flood.BoundarySpaceIds.Count > 0)
                    {
                        int canonical = int.MaxValue;
                        foreach (int boundaryId in flood.BoundarySpaceIds)
                        {
                            if (boundaryId < canonical)
                                canonical = boundaryId;
                        }

                        registry.Absorb(canonical, flood.VisitedFloor);
                    }
                    else
                    {
                        int spaceId = registry.AllocateSpaceId();
                        registry.AssignNew(spaceId, roomKey.BuildingId, roomKey, flood.VisitedFloor);
                    }
                }
            }

            foreach (int spaceId in registry.SpaceIds)
            {
                if (!registry.TryGetSpace(spaceId, out var space))
                    continue;

                if (!_registry.TryGetBuildingExtent(space.BuildingId, out var extent))
                {
                    registry.SetOutdoor(spaceId, true);
                    continue;
                }

                bool outdoor = SpaceLeakEvaluator.Evaluate(
                    registry.GetFloorCells(spaceId),
                    space.BuildingId,
                    extent,
                    index);
                registry.SetOutdoor(spaceId, outdoor);
            }
        }
        static int CompareRoomKeys(RoomKey a, RoomKey b)
        {
            int c = a.BuildingId.CompareTo(b.BuildingId);
            if (c != 0) return c;
            c = a.CellY.CompareTo(b.CellY);
            if (c != 0) return c;
            return a.RoomId.CompareTo(b.RoomId);
        }
    }
}
