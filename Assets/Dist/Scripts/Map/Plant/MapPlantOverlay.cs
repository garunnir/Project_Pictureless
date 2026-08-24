// ============================================================
// MapPlantOverlay — 셀 단위 식물·경작 모델 (청크 TileView와 독립)
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

        public PlantCell(
            Vector3Int cell,
            string seedItemId,
            int plantedWorldMinute,
            bool fertilized = false)
        {
            Cell = cell;
            SeedItemId = seedItemId;
            PlantedWorldMinute = plantedWorldMinute;
            Fertilized = fertilized;
        }
    }

    /// <summary>맵 식물 SSOT. 청크 unload와 무관하게 유지. 분당 growth tick 없음.</summary>
    public sealed class MapPlantOverlay
    {
        readonly Dictionary<Vector3Int, PlantCell> _byCell = new();
        readonly List<PlantCell> _list = new();
        readonly HashSet<Vector3Int> _tilled = new();

        public IReadOnlyList<PlantCell> Plants => _list;
        public int Count => _list.Count;
        public event Action Changed;

        public void Clear()
        {
            if (_byCell.Count == 0 && _tilled.Count == 0)
                return;
            _byCell.Clear();
            _list.Clear();
            _tilled.Clear();
            Changed?.Invoke();
        }

        public bool TryGet(Vector3Int cell, out PlantCell plant) =>
            _byCell.TryGetValue(cell, out plant);

        public bool Contains(Vector3Int cell) => _byCell.ContainsKey(cell);

        public bool IsTilled(Vector3Int cell) => _tilled.Contains(cell);

        public bool TryAdd(Vector3Int cell, string seedItemId, int plantedWorldMinute)
        {
            if (string.IsNullOrEmpty(seedItemId) || _byCell.ContainsKey(cell))
                return false;

            var plant = new PlantCell(cell, seedItemId, plantedWorldMinute);
            _byCell[cell] = plant;
            _list.Add(plant);
            Changed?.Invoke();
            return true;
        }

        public bool TryRemove(Vector3Int cell)
        {
            if (!_byCell.Remove(cell))
                return false;

            for (int i = 0; i < _list.Count; i++)
            {
                if (_list[i].Cell != cell)
                    continue;
                _list.RemoveAt(i);
                break;
            }

            Changed?.Invoke();
            return true;
        }

        public bool TryTill(Vector3Int cell)
        {
            if (!_tilled.Add(cell))
                return false;

            Changed?.Invoke();
            return true;
        }

        public bool TrySetFertilized(Vector3Int cell)
        {
            if (!_byCell.TryGetValue(cell, out PlantCell plant) || plant.Fertilized)
                return false;

            var next = new PlantCell(
                plant.Cell,
                plant.SeedItemId,
                plant.PlantedWorldMinute,
                fertilized: true);
            _byCell[cell] = next;
            for (int i = 0; i < _list.Count; i++)
            {
                if (_list[i].Cell != cell)
                    continue;
                _list[i] = next;
                break;
            }

            Changed?.Invoke();
            return true;
        }

        public void LoadFromDto(IReadOnlyList<PlantCellSaveData> dto)
        {
            _byCell.Clear();
            _list.Clear();
            _tilled.Clear();
            if (dto == null)
            {
                Changed?.Invoke();
                return;
            }

            for (int i = 0; i < dto.Count; i++)
            {
                PlantCellSaveData s = dto[i];
                if (s == null)
                    continue;

                var cell = new Vector3Int(s.cx, s.cy, s.cz);
                if (s.tilled)
                    _tilled.Add(cell);

                if (string.IsNullOrEmpty(s.seedItemId) || _byCell.ContainsKey(cell))
                    continue;

                var plant = new PlantCell(cell, s.seedItemId, s.plantedWorldMinute, s.fertilized);
                _byCell[cell] = plant;
                _list.Add(plant);
            }

            Changed?.Invoke();
        }

        public void WriteToDto(List<PlantCellSaveData> dto)
        {
            if (dto == null)
                return;

            dto.Clear();
            var written = new HashSet<Vector3Int>();
            for (int i = 0; i < _list.Count; i++)
            {
                PlantCell p = _list[i];
                written.Add(p.Cell);
                dto.Add(new PlantCellSaveData
                {
                    cx = p.Cell.x,
                    cy = p.Cell.y,
                    cz = p.Cell.z,
                    seedItemId = p.SeedItemId,
                    plantedWorldMinute = p.PlantedWorldMinute,
                    fertilized = p.Fertilized,
                    tilled = _tilled.Contains(p.Cell),
                });
            }

            foreach (Vector3Int cell in _tilled)
            {
                if (written.Contains(cell))
                    continue;
                dto.Add(new PlantCellSaveData
                {
                    cx = cell.x,
                    cy = cell.y,
                    cz = cell.z,
                    seedItemId = string.Empty,
                    plantedWorldMinute = 0,
                    fertilized = false,
                    tilled = true,
                });
            }
        }
    }
}
