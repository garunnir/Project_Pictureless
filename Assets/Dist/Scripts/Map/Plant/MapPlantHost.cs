// ============================================================
// MapPlantHost — 맵 식물·경작 호스트 (OccupiedCell plant + floor-material till)
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public readonly struct PlantCell
    {
        public readonly Vector3Int Cell;
        public readonly string SeedItemId;
        public readonly int PlantedWorldMinute;
        public readonly bool Fertilized;
        public readonly Guid TileDefId;

        public PlantCell(
            Vector3Int cell,
            string seedItemId,
            int plantedWorldMinute,
            bool fertilized = false,
            Guid tileDefId = default)
        {
            Cell = cell;
            SeedItemId = seedItemId;
            PlantedWorldMinute = plantedWorldMinute;
            Fertilized = fertilized;
            TileDefId = tileDefId;
        }
    }

    [DisallowMultipleComponent]
    public sealed class MapPlantHost : MonoBehaviour
    {
        public static MapPlantHost Runtime { get; private set; }

        public static event Action<MapPlantHost> RuntimeAssigned;
        public static event Action AfterLoaded;

        readonly List<TileData> _cellTilesScratch = new();
        readonly List<PlantCell> _plantListScratch = new();
        TileMapCacheHub _hub;
        TilePrefabDB _prefabDb;
        TileMapController _controller;
        IMapModel _model;
        float _cellSize = 1f;

        public float CellSize => _cellSize;

        /// <summary>Legacy subscribers; plant list is derived from tile model.</summary>
        public MapPlantOverlayBridge Overlay { get; } = new();

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

        public void BindMapContext(
            TileMapCacheHub hub,
            float cellSize,
            TilePrefabDB prefabDb,
            TileMapController controller = null,
            IMapModel model = null)
        {
            _hub = hub;
            _prefabDb = prefabDb;
            _controller = controller;
            _model = model;
            _cellSize = Mathf.Max(1e-4f, cellSize);
        }

        public void LoadFromDto(MapSaveJsonDto dto)
        {
            MigrateLegacyPlantCells(dto);
            if (dto != null)
                dto.plantCells = null;
            AfterLoaded?.Invoke();
        }

        public void WriteToDto(MapSaveJsonDto dto)
        {
            if (dto == null)
                return;
            // Plants persist in tiles[]; clear legacy layer.
            dto.plantCells = null;
        }

        public Vector3Int ResolveCellFromWorld(Vector3 world)
        {
            if (_hub == null)
                return TileHelper.ConvertWorldToGrid(world, _cellSize);

            return OccupiedCellCoord.ResolveFromWorld(_hub, world, _cellSize);
        }

        public bool IsPlantable(Vector3Int cell)
        {
            if (IsTilled(cell))
                return true;
            return CellHasGameplayFlag(cell, TileFlags.Plantable);
        }

        public bool IsTillable(Vector3Int cell)
        {
            if (IsTilled(cell))
                return false;

            TileDefinition def = GetFloorDefinition(cell);
            return TileFlags.HasFlag(def, TileFlags.Plowable) ||
                   TileFlags.HasFlag(def, TileFlags.Diggable);
        }

        public bool IsTilled(Vector3Int cell)
        {
            TileDefinition def = GetFloorDefinition(cell);
            if (def == null)
                return false;
            if (PlantTileIds.IsTilledFloorPrefabId(def.prefabId))
                return true;
            return TileFlags.HasFlag(def, TileFlags.Plantable) &&
                   !TileFlags.HasFlag(def, TileFlags.Plowable);
        }

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

        public bool HasPlant(Vector3Int cell) => TryGetPlantTile(cell, out _);

        public bool TryGetPlant(Vector3Int cell, out PlantCell plant)
        {
            plant = default;
            if (!TryGetPlantTile(cell, out TileData tile))
                return false;

            plant = ToPlantCell(tile);
            return true;
        }

        public void CollectPlants(List<PlantCell> into)
        {
            if (into == null)
                return;
            into.Clear();
            if (_model == null)
                return;

            IReadOnlyList<TileData> snapshot = _model.TilesSnapshot;
            for (int i = 0; i < snapshot.Count; i++)
            {
                TileData tile = snapshot[i];
                if (!tile.plant.HasSeed && !PlantTileIds.IsPlantPrefabId(tile.identity.PrefabId))
                    continue;
                if (!TileIdentityUtil.IsOccupiedCell(tile.identity))
                    continue;
                into.Add(ToPlantCell(tile));
            }
        }

        public bool TryAddPlant(Vector3Int targetCell, string seedItemId, int plantedWorldMinute)
        {
            if (_controller == null || string.IsNullOrEmpty(seedItemId) || HasPlant(targetCell))
                return false;

            Vector3Int installCell = TilePlaceUtil.ResolveOccupiedInstallCell(
                _hub,
                targetCell,
                _cellTilesScratch);
            if (HasPlant(installCell))
                return false;

            if (!TilePrefabDB.TryResolveDefinition(PlantTileIds.PlantSeed, out TileDefinition def) ||
                def == null)
            {
                Debug.LogError($"[MapPlantHost] Missing TileDefinition '{PlantTileIds.PlantSeed}'.");
                return false;
            }

            var plant = new PlantTileInstance
            {
                seedItemId = seedItemId,
                plantedWorldMinute = plantedWorldMinute,
                fertilized = false,
            };
            if (!TilePlaceUtil.TryBuildTileData(def, installCell, out TileData tileData, plant))
                return false;

            _controller.AddAndFlush(tileData);
            Overlay.RaiseChanged();
            return true;
        }

        public bool TryRemovePlant(Vector3Int cell)
        {
            if (_controller == null || !TryGetPlantTile(cell, out TileData tile))
                return false;

            _controller.RemoveAndFlush(tile);
            Overlay.RaiseChanged();
            return true;
        }

        public bool TryTill(Vector3Int cell)
        {
            if (_controller == null || !IsTillable(cell))
                return false;
            if (!TilePrefabDB.TryResolveDefinition(PlantTileIds.FloorTilled, out TileDefinition tilled) ||
                tilled == null)
            {
                Debug.LogError($"[MapPlantHost] Missing TileDefinition '{PlantTileIds.FloorTilled}'.");
                return false;
            }

            if (!_controller.TryReplaceFloorMaterial(cell, tilled))
                return false;

            Overlay.RaiseChanged();
            return true;
        }

        public bool TryFertilize(Vector3Int cell)
        {
            if (_controller == null || !TryGetPlantTile(cell, out TileData tile))
                return false;
            if (tile.plant.fertilized)
                return false;

            var plant = tile.plant;
            plant.fertilized = true;
            // SetTile no-ops when tileDefId+identity match; replace so plant fields persist.
            _controller.RemoveAndFlush(tile);
            var updated = new TileData
            {
                tileDefId = Guid.NewGuid(),
                state = tile.state,
                identity = tile.identity,
                plant = plant,
            };
            _controller.AddAndFlush(updated);
            Overlay.RaiseChanged();
            return true;
        }

        public bool TrySetPlantStage(Vector3Int cell, string stagePrefabId)
        {
            if (_controller == null ||
                string.IsNullOrEmpty(stagePrefabId) ||
                !TryGetPlantTile(cell, out TileData tile))
                return false;
            if (tile.identity.PrefabId == stagePrefabId)
                return true;
            if (!TilePrefabDB.TryResolveDefinition(stagePrefabId, out TileDefinition def) || def == null)
                return false;

            if (!TilePlaceUtil.TryBuildTileData(def, cell, out TileData next, tile.plant))
                return false;

            _controller.RemoveAndFlush(tile);
            _controller.AddAndFlush(next);
            Overlay.RaiseChanged();
            return true;
        }

        public TileDefinition GetFloorDefinition(Vector3Int cell)
        {
            if (_hub == null)
                return null;
            if (!_hub.TryGetFloorFaceForWalkableCell(cell.x, cell.y, cell.z, out TileData face))
                return null;

            return ResolveDefinition(face.identity.PrefabId);
        }

        void MigrateLegacyPlantCells(MapSaveJsonDto dto)
        {
            if (dto?.plantCells == null || dto.plantCells.Count == 0)
                return;
            if (_controller == null)
            {
                Debug.LogWarning("[MapPlantHost] Cannot migrate plantCells — TileMapController missing.");
                return;
            }

            for (int i = 0; i < dto.plantCells.Count; i++)
            {
                PlantCellSaveData s = dto.plantCells[i];
                if (s == null)
                    continue;

                var cell = new Vector3Int(s.cx, s.cy, s.cz);
                if (s.tilled && IsTillable(cell))
                    TryTill(cell);

                if (string.IsNullOrEmpty(s.seedItemId) || HasPlant(cell))
                    continue;

                TryAddPlant(cell, s.seedItemId, s.plantedWorldMinute);
                if (s.fertilized)
                    TryFertilize(cell);
            }
        }

        bool TryGetPlantTile(Vector3Int cell, out TileData tile)
        {
            tile = default;
            if (_hub == null)
                return false;

            _cellTilesScratch.Clear();
            if (!_hub.TryCollectTilesAtOccupiedCell(cell, _cellTilesScratch))
                return false;

            for (int i = 0; i < _cellTilesScratch.Count; i++)
            {
                TileData candidate = _cellTilesScratch[i];
                if (!TileIdentityUtil.IsOccupiedCell(candidate.identity))
                    continue;
                if (candidate.plant.HasSeed || PlantTileIds.IsPlantPrefabId(candidate.identity.PrefabId))
                {
                    tile = candidate;
                    return true;
                }
            }

            return false;
        }

        static PlantCell ToPlantCell(in TileData tile) =>
            new PlantCell(
                tile.identity.GridPos,
                tile.plant.seedItemId,
                tile.plant.plantedWorldMinute,
                tile.plant.fertilized,
                tile.tileDefId);

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

            return TileFlags.HasFlag(GetFloorDefinition(cell), flag);
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
                if (PlantTileIds.IsPlantPrefabId(tile.identity.PrefabId))
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

    /// <summary>Compat shim so MapPlantService can still subscribe Changed / list plants.</summary>
    public sealed class MapPlantOverlayBridge
    {
        public event Action Changed;
        public IReadOnlyList<PlantCell> Plants
        {
            get
            {
                MapPlantHost host = MapPlantHost.Runtime;
                if (host == null)
                    return Array.Empty<PlantCell>();
                var list = new List<PlantCell>();
                host.CollectPlants(list);
                return list;
            }
        }

        public void RaiseChanged() => Changed?.Invoke();
    }
}
