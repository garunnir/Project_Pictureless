using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public class TileMapDtoMapper : IMapMapper
    {
        public MapModelDTO ToPrepared(MapSaveJsonDto tileMapData)
        {
            if (tileMapData == null || tileMapData.tiles == null)
            {
                Debug.LogWarning("TileMapData or its tiles are null.");
                return null;
            }

            List<TileData> prepareData = new List<TileData>();
            bool legacyTiles = tileMapData.schemaVersion < 1;

            foreach (var td in tileMapData.tiles)
            {
                TilePlacementSlot slot = ResolveOccupiedTileSlot(td, legacyTiles);

                if (slot == TilePlacementSlot.HorizontalFace)
                {
                    Debug.LogWarning(
                        $"[TileMapDtoMapper] Floor '{td.prefabId}' in tiles[] is no longer loaded. Use floorFaces[] with anchor GridPos.");
                    continue;
                }

                if (slot == TilePlacementSlot.VerticalFace)
                {
                    byte wallFace = (byte)Mathf.Clamp((int)td.face, 0, 1);
                    if (TryMakeWallFaceIdentity(
                            td.prefabId,
                            new Vector3Int(td.x, td.y, td.z),
                            wallFace,
                            out var identity))
                        prepareData.Add(MakeTile(identity));
                    continue;
                }

                if (TryMakeOccupiedIdentity(
                        td.prefabId,
                        new Vector3Int(td.x, td.y, td.z),
                        out var occupied))
                {
                    var plant = new PlantTileInstance
                    {
                        seedItemId = td.seedItemId,
                        plantedWorldMinute = td.plantedWorldMinute,
                        fertilized = td.fertilized,
                    };
                    prepareData.Add(MakeTile(occupied, plant));
                }
            }

            if (tileMapData.wallEdges != null)
            {
                foreach (var we in tileMapData.wallEdges)
                {
                    if (TryMakeWallFaceIdentity(
                            we.prefabId,
                            new Vector3Int(we.x, we.y, we.z),
                            (byte)Mathf.Clamp((int)we.face, 0, 1),
                            out var identity))
                        prepareData.Add(MakeTile(identity));
                }
            }

            if (tileMapData.floorFaces != null)
            {
                foreach (var ff in tileMapData.floorFaces)
                {
                    if (TryMakeHorizontalFaceIdentity(
                            ff.prefabId,
                            new Vector3Int(ff.x, ff.y, ff.z),
                            out var identity))
                        prepareData.Add(MakeTile(identity));
                }
            }

            return new MapModelDTO(prepareData);
        }

        public MapSaveJsonDto FromPrepared(MapModelDTO prepared)
        {
            IReadOnlyList<TileData> tiles = prepared.TilesData;
            var dto = new MapSaveJsonDto { schemaVersion = 1 };

            foreach (var ti in tiles)
            {
                switch (TileIdentityUtil.GetPlacementSlot(ti.identity))
                {
                    case TilePlacementSlot.VerticalFace:
                        dto.wallEdges.Add(new WallEdgeSaveData
                        {
                            x = ti.identity.GridPos.x,
                            y = ti.identity.GridPos.y,
                            z = ti.identity.GridPos.z,
                            face = ti.identity.wallFace,
                            prefabId = ti.identity.PrefabId,
                        });
                        break;
                    case TilePlacementSlot.HorizontalFace:
                        dto.floorFaces.Add(new FloorFaceSaveData
                        {
                            x = ti.identity.GridPos.x,
                            y = ti.identity.GridPos.y,
                            z = ti.identity.GridPos.z,
                            face = ti.identity.floorFace,
                            prefabId = ti.identity.PrefabId,
                        });
                        break;
                    default:
                        dto.tiles.Add(new TileSaveData
                        {
                            sizeX = ti.identity.sizeUnit.x,
                            sizeY = ti.identity.sizeUnit.y,
                            sizeZ = ti.identity.sizeUnit.z,
                            x = ti.identity.GridPos.x,
                            y = ti.identity.GridPos.y,
                            z = ti.identity.GridPos.z,
                            prefabId = ti.identity.PrefabId,
                            seedItemId = ti.plant.HasSeed ? ti.plant.seedItemId : null,
                            plantedWorldMinute = ti.plant.plantedWorldMinute,
                            fertilized = ti.plant.fertilized,
                        });
                        break;
                }
            }

            return dto;
        }

        static TilePlacementSlot ResolveOccupiedTileSlot(TileSaveData td, bool legacyTiles)
        {
            if (legacyTiles)
            {
                var legacySlot = TileIdentityUtil.InferSlotFromLegacyTileType(
                    NormalizeLegacyTileType(td.tileType));
                if (legacySlot != TilePlacementSlot.None)
                    return legacySlot;
            }

            TilePrefabDB.TryResolveDefinition(td.prefabId, out var def);
            return TileIdentityUtil.ResolvePlacementSlot(def, td.prefabId);
        }

        static TileData MakeTile(in TileIdentity identity) =>
            MakeTile(identity, default);

        static TileData MakeTile(in TileIdentity identity, in PlantTileInstance plant) =>
            new TileData
            {
                tileDefId = Guid.NewGuid(),
                state = new TileState(),
                identity = identity,
                plant = plant,
            };

        static bool TryMakeHorizontalFaceIdentity(string prefabId, Vector3Int anchor, out TileIdentity identity) =>
            TryMakeIdentity(
                prefabId,
                TilePlacementSlot.HorizontalFace,
                anchor,
                wallFace: 0,
                floorFace: (byte)FloorFace.PosY,
                out identity);

        static bool TryMakeWallFaceIdentity(
            string prefabId,
            Vector3Int anchor,
            byte wallFace,
            out TileIdentity identity) =>
            TryMakeIdentity(
                prefabId,
                TilePlacementSlot.VerticalFace,
                anchor,
                wallFace,
                floorFace: 0,
                out identity);

        static bool TryMakeOccupiedIdentity(string prefabId, Vector3Int grid, out TileIdentity identity) =>
            TryMakeIdentity(
                prefabId,
                TilePlacementSlot.OccupiedCell,
                grid,
                wallFace: 0,
                floorFace: 0,
                out identity);

        static bool TryMakeIdentity(
            string prefabId,
            TilePlacementSlot slot,
            Vector3Int gridPos,
            byte wallFace,
            byte floorFace,
            out TileIdentity identity)
        {
            identity = default;
            if (!TilePrefabDB.TryResolveDefinition(prefabId, out var def) || def == null)
            {
                Debug.LogError($"[TileMapDtoMapper] Definition not found for prefabId='{prefabId}'. Tile skipped.");
                return false;
            }

            identity = new TileIdentity
            {
                PrefabId = prefabId,
                GridPos = gridPos,
                sizeUnit = new Vector3Int(
                    Mathf.Max(1, def.size.x),
                    Mathf.Max(1, def.size.y),
                    Mathf.Max(1, def.size.z)),
                placementSlot = (byte)slot,
                wallFace = wallFace,
                floorFace = slot == TilePlacementSlot.HorizontalFace ? (byte)FloorFace.PosY : floorFace,
                collisionFlags = TileCollisionProfile.FromDefinitionForSlot(slot, def),
            };

            if (slot == TilePlacementSlot.VerticalFace &&
                !TileCollisionFlagsUtil.Has(identity.collisionFlags, TileCollisionFlags.OccludesEdge))
            {
                Debug.LogWarning(
                    $"[TileMapDtoMapper] VerticalFace '{prefabId}' has no OccludesEdge flag. BFS character occlusion will skip this edge.");
            }

            return true;
        }

        static byte NormalizeLegacyTileType(byte raw)
        {
            if (raw == 3)
                return (byte)TileView.TileType.Wall;
            return raw;
        }
    }
}
