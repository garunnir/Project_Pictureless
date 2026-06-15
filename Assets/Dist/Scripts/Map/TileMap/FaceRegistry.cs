// ============================================================

// FaceRegistry — 면 타일 Dictionary + 셀 incident 인덱스

// ============================================================

using System;

using System.Collections.Generic;

using UnityEngine;



namespace IsoTilemap

{

    sealed class FaceRegistry<TKey> where TKey : IEquatable<TKey>

    {

        readonly Dictionary<TKey, TileData> _faces = new Dictionary<TKey, TileData>();

        readonly Dictionary<Guid, TKey> _idToKey = new Dictionary<Guid, TKey>();

        readonly Dictionary<Vector3Int, List<TKey>> _cellToKeys = new Dictionary<Vector3Int, List<TKey>>();

        readonly Action<TKey, TileData, List<Vector3Int>> _appendIncidents;

        readonly List<Vector3Int> _incidentScratch = new List<Vector3Int>(4);



        public FaceRegistry(Action<TKey, TileData, List<Vector3Int>> appendIncidents) =>

            _appendIncidents = appendIncidents ?? throw new ArgumentNullException(nameof(appendIncidents));



        public IReadOnlyDictionary<TKey, TileData> Index => _faces;



        public void Clear()

        {

            _faces.Clear();

            _idToKey.Clear();

            _cellToKeys.Clear();

        }



        public void Register(in TileData tile, TKey key)

        {

            if (_faces.ContainsKey(key))

                RemoveInternal(key);



            _faces[key] = tile;

            _idToKey[tile.tileDefId] = key;

            AddIncident(key, tile);

        }



        public bool TryGetTile(in TKey key, out TileData tile) => _faces.TryGetValue(key, out tile);



        public bool TryRemove(in TKey key, out TileData removed)

        {

            if (!_faces.TryGetValue(key, out removed))

                return false;



            RemoveInternal(key);

            return true;

        }



        public bool TryRemove(Guid tileId, out TileData removed)

        {

            if (!_idToKey.TryGetValue(tileId, out var key))

            {

                removed = default;

                return false;

            }



            removed = _faces[key];

            RemoveInternal(key);

            return true;

        }



        public bool TryReplaceTileData(in TileData tile, Func<TileData, TKey> keyFromTile)

        {

            var key = keyFromTile(tile);

            if (_faces.TryGetValue(key, out var existing) && existing.tileDefId == tile.tileDefId)

            {

                _faces[key] = tile;

                return true;

            }



            if (!_idToKey.TryGetValue(tile.tileDefId, out var foundKey))

                return false;



            RemoveInternal(foundKey);

            Register(tile, key);

            return true;

        }



        public void AppendAtCell(Vector3Int cell, List<TileData> appendTo)

        {

            if (!_cellToKeys.TryGetValue(cell, out var keys))

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



        public void CopyTilesTo(List<TileData> buffer)

        {

            foreach (var kv in _faces)

                buffer.Add(kv.Value);

        }



        void AddIncident(in TKey key, in TileData tile)

        {

            _incidentScratch.Clear();

            _appendIncidents(key, tile, _incidentScratch);

            for (int i = 0; i < _incidentScratch.Count; i++)

                TouchCell(_incidentScratch[i], key);

        }



        void TouchCell(Vector3Int cell, in TKey key)

        {

            if (!_cellToKeys.TryGetValue(cell, out var list))

            {

                list = new List<TKey>(2);

                _cellToKeys[cell] = list;

            }



            if (!list.Contains(key))

                list.Add(key);

        }



        void RemoveInternal(in TKey key)

        {

            if (!_faces.TryGetValue(key, out var tile))

                return;



            _incidentScratch.Clear();

            _appendIncidents(key, tile, _incidentScratch);

            for (int i = 0; i < _incidentScratch.Count; i++)

                DetachCell(_incidentScratch[i], key);



            _faces.Remove(key);

            _idToKey.Remove(tile.tileDefId);

        }



        void DetachCell(Vector3Int cell, in TKey key)

        {

            if (!_cellToKeys.TryGetValue(cell, out var list))

                return;



            list.Remove(key);

            if (list.Count == 0)

                _cellToKeys.Remove(cell);

        }



        internal static void AppendWallIncidents(WallEdgeKey key, TileData tile, List<Vector3Int> into) =>

            TileIdentityUtil.AppendWallIncidentCells(key, tile.identity.sizeUnit.y, into);



        internal static void AppendFloorIncidents(FloorFaceKey key, TileData tile, List<Vector3Int> into) =>

            TileIdentityUtil.AppendFloorIncidentCells(key, tile.identity.sizeUnit.y, into);

    }

}

