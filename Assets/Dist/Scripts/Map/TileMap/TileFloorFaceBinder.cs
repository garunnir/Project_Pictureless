using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// 그리드 셀 데이터와 분리된 층 사이 Floor face 레지스트리.
    /// 물리적으로 같은 수평 면은 하나의 키(<see cref="FloorFaceKey"/>)로만 보관됩니다.
    /// </summary>
    public sealed class TileFloorFaceBinder : ITileFloorFaceBinderReadOnly
    {
        readonly Dictionary<FloorFaceKey, TileData> _faces = new Dictionary<FloorFaceKey, TileData>();
        readonly Dictionary<Vector3Int, List<FloorFaceKey>> _cellToFaceKeys = new Dictionary<Vector3Int, List<FloorFaceKey>>();

        public IReadOnlyDictionary<FloorFaceKey, TileData> FaceIndex => _faces;

        public void Clear()
        {
            _faces.Clear();
            _cellToFaceKeys.Clear();
        }

        public void Register(in TileData tile)
        {
            var key = FloorFaceKey.FromFloorTileIdentity(tile.identity);
            if (_faces.ContainsKey(key))
                RemoveInternal(key);

            _faces[key] = tile;
            AddIncident(key, tile);
        }

        public bool TryGetTile(in FloorFaceKey key, out TileData tile) => _faces.TryGetValue(key, out tile);

        public bool TryRemove(Guid tileId, out TileData removed)
        {
            foreach (var kv in _faces)
            {
                if (kv.Value.tileDefId != tileId)
                    continue;

                removed = kv.Value;
                RemoveInternal(kv.Key);
                return true;
            }

            removed = default;
            return false;
        }

        public bool TryReplaceTileData(in TileData tile)
        {
            var key = FloorFaceKey.FromFloorTileIdentity(tile.identity);
            if (_faces.TryGetValue(key, out var existing) && existing.tileDefId == tile.tileDefId)
            {
                _faces[key] = tile;
                return true;
            }

            FloorFaceKey? found = null;
            foreach (var kv in _faces)
            {
                if (kv.Value.tileDefId != tile.tileDefId)
                    continue;
                found = kv.Key;
                break;
            }

            if (found == null)
                return false;

            RemoveInternal(found.Value);
            Register(tile);
            return true;
        }

        public void AppendIncidentFaces(Vector3Int cell, List<TileData> appendTo)
        {
            if (!_cellToFaceKeys.TryGetValue(cell, out var keys))
                return;

            for (int i = 0; i < keys.Count; i++)
            {
                if (_faces.TryGetValue(keys[i], out var td))
                    appendTo.Add(td);
            }
        }

        public IEnumerable<TileData> EnumerateTiles()
        {
            foreach (var kv in _faces)
                yield return kv.Value;
        }

        void AddIncident(in FloorFaceKey key, in TileData tile)
        {
            int sy = Mathf.Max(1, tile.identity.sizeUnit.y);
            for (int dy = 0; dy < sy; dy++)
            {
                var yOffset = new Vector3Int(0, dy, 0);
                TouchCell(key.Anchor + yOffset, key);
                TouchCell(key.CellAbove + yOffset, key);
            }
        }

        void TouchCell(Vector3Int cell, in FloorFaceKey key)
        {
            if (!_cellToFaceKeys.TryGetValue(cell, out var list))
            {
                list = new List<FloorFaceKey>(2);
                _cellToFaceKeys[cell] = list;
            }

            if (!list.Contains(key))
                list.Add(key);
        }

        void RemoveInternal(in FloorFaceKey key)
        {
            if (!_faces.TryGetValue(key, out var tile))
                return;

            int sy = Mathf.Max(1, tile.identity.sizeUnit.y);
            for (int dy = 0; dy < sy; dy++)
            {
                var yOffset = new Vector3Int(0, dy, 0);
                DetachCell(key.Anchor + yOffset, key);
                DetachCell(key.CellAbove + yOffset, key);
            }

            _faces.Remove(key);
        }

        void DetachCell(Vector3Int cell, in FloorFaceKey key)
        {
            if (!_cellToFaceKeys.TryGetValue(cell, out var list))
                return;

            list.Remove(key);
            if (list.Count == 0)
                _cellToFaceKeys.Remove(cell);
        }
    }
}
