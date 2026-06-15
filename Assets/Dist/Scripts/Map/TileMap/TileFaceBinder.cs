// ============================================================
// TileFaceBinder — 수직·수평 면 타일 레지스트리 (셀 dict와 분리)
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public sealed class TileFaceBinder : ITileFaceBinderReadOnly
    {
        readonly FaceRegistry<WallEdgeKey> _wallFaces =
            new FaceRegistry<WallEdgeKey>(FaceRegistry<WallEdgeKey>.AppendWallIncidents);

        readonly FaceRegistry<FloorFaceKey> _floorFaces =
            new FaceRegistry<FloorFaceKey>(FaceRegistry<FloorFaceKey>.AppendFloorIncidents);

        public IReadOnlyDictionary<WallEdgeKey, TileData> WallFaceIndex => _wallFaces.Index;
        public IReadOnlyDictionary<FloorFaceKey, TileData> FloorFaceIndex => _floorFaces.Index;

        public void Clear()
        {
            _wallFaces.Clear();
            _floorFaces.Clear();
        }

        public void Register(in TileData tile)
        {
            switch (TileIdentityUtil.GetPlacementSlot(tile.identity))
            {
                case TilePlacementSlot.VerticalFace:
                    _wallFaces.Register(tile, WallEdgeKey.FromWallTileIdentity(tile.identity));
                    break;
                case TilePlacementSlot.HorizontalFace:
                    _floorFaces.Register(tile, FloorFaceKey.FromFloorTileIdentity(tile.identity));
                    break;
            }
        }

        public bool TryGetWallFace(in WallEdgeKey key, out TileData tile) =>
            _wallFaces.TryGetTile(key, out tile);

        public bool TryGetFloorFace(in FloorFaceKey key, out TileData tile) =>
            _floorFaces.TryGetTile(key, out tile);

        public bool TryRemoveWall(in WallEdgeKey key, out TileData removed) =>
            _wallFaces.TryRemove(key, out removed);

        public bool TryRemoveFloor(in FloorFaceKey key, out TileData removed) =>
            _floorFaces.TryRemove(key, out removed);

        public bool TryRemove(Guid tileId, out TileData removed) =>
            _wallFaces.TryRemove(tileId, out removed)
            || _floorFaces.TryRemove(tileId, out removed);

        public bool TryReplaceTileData(in TileData tile)
        {
            switch (TileIdentityUtil.GetPlacementSlot(tile.identity))
            {
                case TilePlacementSlot.VerticalFace:
                    return _wallFaces.TryReplaceTileData(tile, t => WallEdgeKey.FromWallTileIdentity(t.identity));
                case TilePlacementSlot.HorizontalFace:
                    return _floorFaces.TryReplaceTileData(tile, t => FloorFaceKey.FromFloorTileIdentity(t.identity));
                default:
                    return false;
            }
        }

        public void AppendFacesAtCell(Vector3Int cell, List<TileData> appendTo)
        {
            _wallFaces.AppendAtCell(cell, appendTo);
            _floorFaces.AppendAtCell(cell, appendTo);
        }

        public void AppendWallFacesAtCell(Vector3Int cell, List<TileData> appendTo) =>
            _wallFaces.AppendAtCell(cell, appendTo);

        public void AppendFloorFacesAtCell(Vector3Int cell, List<TileData> appendTo) =>
            _floorFaces.AppendAtCell(cell, appendTo);

        public void CopyWallFacesTo(List<TileData> buffer)
        {
            buffer.Clear();
            _wallFaces.CopyTilesTo(buffer);
        }

        public void CopyFloorFacesTo(List<TileData> buffer)
        {
            buffer.Clear();
            _floorFaces.CopyTilesTo(buffer);
        }

        public IEnumerable<TileData> EnumerateFaceTiles()
        {
            foreach (var t in _wallFaces.EnumerateTiles())
                yield return t;
            foreach (var t in _floorFaces.EnumerateTiles())
                yield return t;
        }
    }
}
