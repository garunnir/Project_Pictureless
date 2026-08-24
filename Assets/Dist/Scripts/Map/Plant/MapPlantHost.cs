// ============================================================
// MapPlantHost — 맵 식물 런타임 호스트 (로드·세이브·셀 API)
// ============================================================
// flowchart LR
//   Json[Map JSON plantCells+clock] --> Host[MapPlantHost]
//   Host --> Overlay[MapPlantOverlay model]
//   Overlay --> View[Dist overlay GO]
//   Plant[Inventory Plant] --> Host
//   View --> Harvest[Tile harvest]

using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    [DisallowMultipleComponent]
    public sealed class MapPlantHost : MonoBehaviour
    {
        public static MapPlantHost Runtime { get; private set; }

        public static event Action<MapPlantHost> RuntimeAssigned;
        public static event Action AfterLoaded;

        readonly MapPlantOverlay _overlay = new();
        readonly List<TileData> _cellTilesScratch = new();
        TileMapCacheHub _hub;
        TilePrefabDB _prefabDb;
        float _cellSize = 1f;

        public MapPlantOverlay Overlay => _overlay;
        public float CellSize => _cellSize;

        void Awake()
        {
            Runtime = this;
            RuntimeAssigned?.Invoke(this);
        }

        void OnDestroy()
        {
            if (ReferenceEquals(Runtime, this))
                Runtime = null;
        }

        public void BindMapContext(TileMapCacheHub hub, float cellSize, TilePrefabDB prefabDb)
        {
            _hub = hub;
            _prefabDb = prefabDb;
            _cellSize = Mathf.Max(1e-4f, cellSize);
        }

        public void LoadFromDto(MapSaveJsonDto dto)
        {
            if (dto?.plantCells == null)
            {
                _overlay.Clear();
                AfterLoaded?.Invoke();
                return;
            }

            _overlay.LoadFromDto(dto.plantCells);
            AfterLoaded?.Invoke();
        }

        public void WriteToDto(MapSaveJsonDto dto)
        {
            if (dto == null)
                return;
            dto.plantCells ??= new List<PlantCellSaveData>();
            _overlay.WriteToDto(dto.plantCells);
        }

        public Vector3Int ResolveCellFromWorld(Vector3 world)
        {
            if (_hub == null)
                return TileHelper.ConvertWorldToGrid(world, _cellSize);

            return OccupiedCellCoord.ResolveFromWorld(_hub, world, _cellSize);
        }

        public bool IsPlantable(Vector3Int cell)
        {
            if (_overlay.IsTilled(cell))
                return true;
            return CellHasGameplayFlag(cell, TileFlags.Plantable);
        }

        public bool IsTillable(Vector3Int cell)
        {
            if (_overlay.IsTilled(cell))
                return false;

            TileDefinition def = GetFloorDefinition(cell);
            return TileFlags.HasFlag(def, TileFlags.Plowable) ||
                   TileFlags.HasFlag(def, TileFlags.Diggable);
        }

        public bool IsTilled(Vector3Int cell) => _overlay.IsTilled(cell);

        public bool IsOutdoorCell(Vector3Int cell)
        {
            if (_hub == null)
                return true;
            return _hub.IsOutdoorEvaluation(cell.y, cell.x, cell.z);
        }

        public bool IsGreenhouseCell(Vector3Int cell)
        {
            if (CellHasGameplayFlag(cell, MapPlantConsts.GreenhouseFlag))
                return true;
            return OccupiedFurnitureHasFlag(cell, TileFlags.Plantable);
        }

        public bool HasPlant(Vector3Int cell) => _overlay.Contains(cell);

        public bool TryGetPlant(Vector3Int cell, out PlantCell plant) =>
            _overlay.TryGet(cell, out plant);

        public bool TryAddPlant(Vector3Int cell, string seedItemId, int plantedWorldMinute) =>
            _overlay.TryAdd(cell, seedItemId, plantedWorldMinute);

        public bool TryRemovePlant(Vector3Int cell) => _overlay.TryRemove(cell);

        public bool TryTill(Vector3Int cell) =>
            IsTillable(cell) && _overlay.TryTill(cell);

        public bool TryFertilize(Vector3Int cell) => _overlay.TrySetFertilized(cell);

        TileDefinition GetFloorDefinition(Vector3Int cell)
        {
            if (_hub == null)
                return null;
            if (!_hub.TryGetFloorFaceForWalkableCell(cell.x, cell.y, cell.z, out TileData face))
                return null;

            return ResolveDefinition(face.identity.PrefabId);
        }

        bool CellHasGameplayFlag(Vector3Int cell, string flag)
        {
            if (_hub == null)
                return TileFlags.HasFlag(GetFloorDefinition(cell), flag);

            _cellTilesScratch.Clear();
            if (!_hub.TryCollectTilesAtOccupiedCell(cell, _cellTilesScratch))
                return TileFlags.HasFlag(GetFloorDefinition(cell), flag);

            for (int i = 0; i < _cellTilesScratch.Count; i++)
            {
                TileDefinition def = ResolveDefinition(_cellTilesScratch[i].identity.PrefabId);
                if (TileFlags.HasFlag(def, flag))
                    return true;
            }

            return false;
        }

        bool OccupiedFurnitureHasFlag(Vector3Int cell, string flag)
        {
            if (_hub == null)
                return false;

            _cellTilesScratch.Clear();
            if (!_hub.TryCollectTilesAtOccupiedCell(cell, _cellTilesScratch))
                return false;

            for (int i = 0; i < _cellTilesScratch.Count; i++)
            {
                TileData tile = _cellTilesScratch[i];
                if (!TileIdentityUtil.IsOccupiedCell(tile.identity))
                    continue;

                TileDefinition def = ResolveDefinition(tile.identity.PrefabId);
                if (TileFlags.HasFlag(def, flag))
                    return true;
            }

            return false;
        }

        TileDefinition ResolveDefinition(string prefabId)
        {
            TileDefinition def = null;
            if (_prefabDb != null)
                _prefabDb.TryGetDefinition(prefabId, out def);
            else
                TilePrefabDB.TryResolveDefinition(prefabId, out def);
            return def;
        }
    }
}
