// ============================================================
// MapLiquidOverlay — 맵 액체 스파스 저장소 (dirty 큐 + 시드 + save/load)
// ============================================================
// 계약(migration-parity/정적 셀 무연산 보증, docs/map/LIQUID.md 참조):
// - Seed(SeedFromAuthoringFaces/SeedEffectiveMl)는 절대 dirty를 등록하지 않는다.
// - dirty 진입점은 MarkDirty 호출자(FlowSolver 전파, MlBridge Pour/Draw)뿐 — 매 틱 전체 스캔 금지.
// - EffectiveMl == 0인 셀은 사전에서 제거해 스파스 상태를 유지한다.
// - 렌더 통지(CellChanged/BulkChanged)는 sim dirty 큐와 별개다. 시드는 셀 단위로 통지하지 않고
//   BulkChanged 1회만 보낸다 — 바다맵 시드에서 수십만 건 통지가 나가지 않도록.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public sealed class MapLiquidOverlay
    {
        readonly Dictionary<Vector3Int, MapLiquidCell> _cells = new();
        readonly Queue<Vector3Int> _dirtyQueue = new();
        readonly HashSet<Vector3Int> _dirtySet = new();

        // 열 확산은 흐름과 파급 조건이 달라(온도 기울기 vs 부피 차) 큐를 공유하면 서로를 깨운다.
        readonly Queue<Vector3Int> _thermalDirtyQueue = new();
        readonly HashSet<Vector3Int> _thermalDirtySet = new();

        public IReadOnlyDictionary<Vector3Int, MapLiquidCell> Cells => _cells;
        public int DirtyCount => _dirtyQueue.Count;
        public int ThermalDirtyCount => _thermalDirtyQueue.Count;

        /// <summary>단일 셀의 보유량이 바뀜 — 렌더러가 해당 셀이 속한 청크만 무효화한다.</summary>
        public event Action<Vector3Int> CellChanged;

        /// <summary>Clear/Load/Seed 등 대량 교체 — 렌더러가 전체를 무효화한다.</summary>
        public event Action BulkChanged;

        public void Clear()
        {
            _cells.Clear();
            _dirtyQueue.Clear();
            _dirtySet.Clear();
            _thermalDirtyQueue.Clear();
            _thermalDirtySet.Clear();
            BulkChanged?.Invoke();
        }

        public bool TryGetCell(Vector3Int cell, out MapLiquidCell liquidCell) =>
            _cells.TryGetValue(cell, out liquidCell);

        public int GetEffectiveMl(Vector3Int cell) =>
            _cells.TryGetValue(cell, out MapLiquidCell c) ? c.EffectiveMl : 0;

        /// <summary>흐름·붓기/뜨기로 인한 변화. 호출부가 필요 시 MarkDirty를 별도로 호출한다.</summary>
        public void AddEffectiveMl(Vector3Int cell, int deltaMl, string typeId)
        {
            if (deltaMl == 0)
                return;

            if (_cells.TryGetValue(cell, out MapLiquidCell c))
            {
                int next = c.EffectiveMl + deltaMl;
                if (next <= 0)
                {
                    _cells.Remove(cell);
                    CellChanged?.Invoke(cell);
                    return;
                }

                c.SetEffectiveMl(next);
                CellChanged?.Invoke(cell);
                return;
            }

            if (deltaMl <= 0)
                return;

            _cells[cell] = MapLiquidCell.FromEffectiveMl(typeId ?? MapLiquidConsts.WaterTypeId, deltaMl);
            CellChanged?.Invoke(cell);
        }

        /// <summary>맵 로드 1회 시드 전용 — dirty 큐를 절대 건드리지 않는다(정적 셀 무연산 보증 §1).</summary>
        public void SeedEffectiveMl(Vector3Int cell, string typeId, int effectiveMl)
        {
            if (effectiveMl <= 0)
                return;

            _cells[cell] = MapLiquidCell.FromEffectiveMl(
                typeId,
                effectiveMl,
                MapLiquidAmbient.ResolveDeciC(cell));
        }

        public void MarkDirty(Vector3Int cell)
        {
            if (_dirtySet.Add(cell))
                _dirtyQueue.Enqueue(cell);
        }

        public void MarkThermalDirty(Vector3Int cell)
        {
            if (_thermalDirtySet.Add(cell))
                _thermalDirtyQueue.Enqueue(cell);
        }

        /// <summary>ThermalSolver 전용 — 큐가 비면 false(=이번 틱 처리할 게 없음, 비용 0).</summary>
        public bool TryPopThermalDirty(out Vector3Int cell)
        {
            if (_thermalDirtyQueue.Count == 0)
            {
                cell = default;
                return false;
            }

            cell = _thermalDirtyQueue.Dequeue();
            _thermalDirtySet.Remove(cell);
            return true;
        }

        /// <summary>보유량 변화 없이 표현만 바뀐 경우(상변화 등) 렌더러에 알린다.</summary>
        public void RaiseCellChanged(Vector3Int cell) => CellChanged?.Invoke(cell);

        /// <summary>FlowSolver 전용 — dirty 큐 pop. 큐가 비면 false(=이번 틱 처리할 게 없음, 비용 0).</summary>
        public bool TryPopDirty(out Vector3Int cell)
        {
            if (_dirtyQueue.Count == 0)
            {
                cell = default;
                return false;
            }

            cell = _dirtyQueue.Dequeue();
            _dirtySet.Remove(cell);
            return true;
        }

        /// <summary>
        /// 맵 로드 시 물 저작 면(liquidAuthoringFaces)으로부터 1회 시드.
        /// 이미 저장된 liquidCells가 있으면(호출 전 LoadFromDto 완료) 호출부가 스킵을 책임진다.
        /// 타일 모델을 순회하지 않는다 — 물은 TileData가 아니다.
        /// </summary>
        public void SeedFromAuthoringFaces(IReadOnlyList<FloorFaceSaveData> authoringFaces)
        {
            foreach (var kv in MapLiquidAuthoringBake.ResolveAuthoringCells(authoringFaces))
                SeedEffectiveMl(kv.Key, MapLiquidConsts.WaterTypeId, kv.Value);

            BulkChanged?.Invoke();
        }

        /// <summary>
        /// <paramref name="hasTemperature"/>가 false면 저장된 tempDeciC를 신뢰하지 않고 기본 기온으로 초기화한다 —
        /// 구 JSON의 0은 물의 어는점과 겹쳐 바다 전체가 얼어버린다.
        /// </summary>
        public void LoadFromDto(IReadOnlyList<MapLiquidCellSaveData> dto, bool hasTemperature)
        {
            Clear();
            if (dto == null)
                return;

            for (int i = 0; i < dto.Count; i++)
            {
                MapLiquidCellSaveData d = dto[i];
                if (d == null || string.IsNullOrEmpty(d.typeId))
                    continue;
                if (d.level == 0 && d.remainderMl == 0)
                    continue;

                short tempDeciC = hasTemperature ? d.tempDeciC : MapLiquidConsts.DefaultAmbientDeciC;
                var cell = new Vector3Int(d.x, d.y, d.z);
                _cells[cell] = new MapLiquidCell(d.typeId, d.level, d.remainderMl, tempDeciC);
            }

            BulkChanged?.Invoke();
        }

        public void WriteToDto(List<MapLiquidCellSaveData> dto)
        {
            if (dto == null)
                return;

            dto.Clear();
            foreach (var kv in _cells)
            {
                MapLiquidCell c = kv.Value;
                if (c.IsEmpty)
                    continue;

                dto.Add(new MapLiquidCellSaveData
                {
                    x = kv.Key.x,
                    y = kv.Key.y,
                    z = kv.Key.z,
                    typeId = c.TypeId,
                    level = c.Level,
                    remainderMl = c.RemainderMl,
                    tempDeciC = c.TempDeciC,
                });
            }
        }
    }
}
