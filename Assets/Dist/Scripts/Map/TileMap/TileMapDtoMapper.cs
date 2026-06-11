using System;
using System.Collections.Generic;
using UnityEngine;
namespace IsoTilemap
{
    //Json->DTO
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
            foreach (var td in tileMapData.tiles)
            {
                byte tileType = NormalizeTileType(td.tileType);
                if (tileType == (byte)TileView.TileType.Floor)
                {
                    if (!TryAddFloorFaceFromLegacyTile(td, prepareData))
                        continue;
                    continue;
                }

                byte edgeFace = TileIdentity.EdgeFaceNone;
                if (tileType == (byte)TileView.TileType.EdgeWall)
                    edgeFace = (byte)Mathf.Clamp((int)td.face, 0, 1);

                if (!TryBakeFromDefinition(td.prefabId, tileType, out var sizeUnit, out byte collisionFlags))
                    continue;

                prepareData.Add(new TileData
                {
                    tileDefId = Guid.NewGuid(),
                    state = new TileState { },
                    identity = new TileIdentity
                    {
                        PrefabId = td.prefabId,
                        tileType = tileType,
                        GridPos = new Vector3Int(td.x, td.y, td.z),
                        sizeUnit = sizeUnit,
                        edgeFace = edgeFace,
                        floorFace = TileIdentity.FloorFaceNone,
                        collisionFlags = collisionFlags,
                    }
                });
            }

            if (tileMapData.wallEdges != null)
            {
                foreach (var we in tileMapData.wallEdges)
                {
                    if (!TryBakeFromDefinition(
                            we.prefabId,
                            (byte)TileView.TileType.EdgeWall,
                            out var sizeUnit,
                            out byte collisionFlags))
                        continue;

                    byte faceClamped = (byte)Mathf.Clamp((int)we.face, 0, 1);
                    prepareData.Add(new TileData
                    {
                        tileDefId = Guid.NewGuid(),
                        state = new TileState(),
                        identity = new TileIdentity
                        {
                            PrefabId = we.prefabId,
                            GridPos = new Vector3Int(we.x, we.y, we.z),
                            sizeUnit = sizeUnit,
                            tileType = (byte)TileView.TileType.EdgeWall,
                            edgeFace = faceClamped,
                            floorFace = TileIdentity.FloorFaceNone,
                            collisionFlags = collisionFlags,
                        }
                    });
                }
            }

            if (tileMapData.floorFaces != null)
            {
                foreach (var ff in tileMapData.floorFaces)
                {
                    if (!TryBakeFromDefinition(
                            ff.prefabId,
                            (byte)TileView.TileType.Floor,
                            out var sizeUnit,
                            out byte collisionFlags))
                        continue;

                    prepareData.Add(new TileData
                    {
                        tileDefId = Guid.NewGuid(),
                        state = new TileState(),
                        identity = new TileIdentity
                        {
                            PrefabId = ff.prefabId,
                            GridPos = new Vector3Int(ff.x, ff.y, ff.z),
                            sizeUnit = sizeUnit,
                            tileType = (byte)TileView.TileType.Floor,
                            edgeFace = TileIdentity.EdgeFaceNone,
                            floorFace = (byte)FloorFace.PosY,
                            collisionFlags = collisionFlags,
                        }
                    });
                }
            }

            return new MapModelDTO(prepareData);
        }

        public MapSaveJsonDto FromPrepared(MapModelDTO prepared)
        {
            IReadOnlyList<TileData> tiles = prepared.TilesData;
            MapSaveJsonDto tile = new MapSaveJsonDto();

            foreach (var ti in tiles)
            {
                if (ti.identity.tileType == (byte)TileView.TileType.EdgeWall)
                {
                    tile.wallEdges.Add(new WallEdgeSaveData
                    {
                        x = ti.identity.GridPos.x,
                        y = ti.identity.GridPos.y,
                        z = ti.identity.GridPos.z,
                        face = ti.identity.edgeFace,
                        prefabId = ti.identity.PrefabId,
                    });
                }
                else if (ti.identity.tileType == (byte)TileView.TileType.Floor)
                {
                    tile.floorFaces.Add(new FloorFaceSaveData
                    {
                        x = ti.identity.GridPos.x,
                        y = ti.identity.GridPos.y,
                        z = ti.identity.GridPos.z,
                        face = ti.identity.floorFace,
                        prefabId = ti.identity.PrefabId,
                    });
                }
                else
                {
                    tile.tiles.Add(new TileSaveData
                    {
                        sizeX = ti.identity.sizeUnit.x,
                        sizeY = ti.identity.sizeUnit.y,
                        sizeZ = ti.identity.sizeUnit.z,
                        x = ti.identity.GridPos.x,
                        y = ti.identity.GridPos.y,
                        z = ti.identity.GridPos.z,
                        tileType = ti.identity.tileType,
                        prefabId = ti.identity.PrefabId,
                    });
                }
            }

            return tile;
        }

        static bool TryAddFloorFaceFromLegacyTile(TileSaveData td, List<TileData> prepareData)
        {
            if (!TryBakeFromDefinition(
                    td.prefabId,
                    (byte)TileView.TileType.Floor,
                    out var sizeUnit,
                    out byte collisionFlags))
                return false;

            Vector3Int walkable = new Vector3Int(td.x, td.y, td.z);
            var key = FloorFaceKey.ForWalkableCell(walkable);
            prepareData.Add(new TileData
            {
                tileDefId = Guid.NewGuid(),
                state = new TileState(),
                identity = new TileIdentity
                {
                    PrefabId = td.prefabId,
                    GridPos = key.Anchor,
                    sizeUnit = sizeUnit,
                    tileType = (byte)TileView.TileType.Floor,
                    edgeFace = TileIdentity.EdgeFaceNone,
                    floorFace = (byte)FloorFace.PosY,
                    collisionFlags = collisionFlags,
                }
            });
            return true;
        }

        static byte NormalizeTileType(byte raw)
        {
            // legacy Obstacle(3) → Wall(2)
            if (raw == 3)
                return (byte)TileView.TileType.Wall;
            return raw;
        }

        static bool TryBakeFromDefinition(
            string prefabId,
            byte tileType,
            out Vector3Int sizeUnit,
            out byte collisionFlags)
        {
            sizeUnit = Vector3Int.one;
            collisionFlags = 0;

            if (!TilePrefabDB.TryResolveDefinition(prefabId, out var def) || def == null)
            {
                Debug.LogError($"[TileMapDtoMapper] Definition not found for prefabId='{prefabId}'. Tile skipped.");
                return false;
            }

            sizeUnit = new Vector3Int(
                Mathf.Max(1, def.size.x),
                Mathf.Max(1, def.size.y),
                Mathf.Max(1, def.size.z));
            collisionFlags = TileCollisionProfile.FromDefinitionForTileType(tileType, def);

            if (tileType == (byte)TileView.TileType.EdgeWall &&
                !TileCollisionFlagsUtil.Has(collisionFlags, TileCollisionFlags.OccludesEdge))
            {
                Debug.LogWarning(
                    $"[TileMapDtoMapper] EdgeWall '{prefabId}' has no OccludesEdge flag. BFS character occlusion will skip this edge.");
            }

            return true;
        }
    }
}
