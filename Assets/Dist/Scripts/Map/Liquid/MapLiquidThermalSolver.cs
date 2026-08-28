// ============================================================
// MapLiquidThermalSolver — 액체 온도 확산 CA + 상변화 교차 통지
// ============================================================
// FlowSolver와 같은 계약: thermal dirty 큐 pop 외의 순회를 하지 않는다.
// 평형에 도달한 셀은 delta가 게이트에서 0으로 잘려 자신·이웃을 재등록하지 않으므로,
// 정지된 바다는 맵 크기와 무관하게 이 호출 자체가 no-op다(정적 셀 무연산 보증).
//
// 유일한 예외는 MarkAmbientBoundaryDirty — 기온이 임계 이상 움직였을 때만 부르는
// O(액체 셀) 재표집이다. 매 틱 경로가 아니며, 호출부가 AmbientResampleStepDeciC로 막는다.
//
// 단순화(문서화된 한계): 액체가 이동해도 온도는 따라가지 않는다(advection 없음).
// 이동 후 relax로 수렴하므로 정지 상태의 결과는 같고, 과도 구간만 다르다.

using UnityEngine;

namespace IsoTilemap
{
    public sealed class MapLiquidThermalSolver
    {
        static readonly Vector3Int[] Neighbors =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
        };

        readonly MapLiquidOverlay _overlay;
        TileMapCacheHub _hub;
        short _lastAmbientDeciC;
        bool _ambientSampled;

        public MapLiquidThermalSolver(MapLiquidOverlay overlay) => _overlay = overlay;

        public void BindMapContext(TileMapCacheHub hub) => _hub = hub;

        /// <summary>WorldClock.MinuteChanged 1회당 호출. thermal dirty 큐가 비었으면 즉시 반환(비용 0).</summary>
        public void ProcessDirty(int maxUpdates)
        {
            int processed = 0;
            while (processed < maxUpdates && _overlay.TryPopThermalDirty(out Vector3Int cell))
            {
                ProcessCell(cell);
                processed++;
            }
        }

        /// <summary>
        /// 기온이 <see cref="MapLiquidConsts.AmbientResampleStepDeciC"/> 이상 움직였을 때만
        /// 노출 셀을 다시 dirty로 넣는다. 매 분 호출해도 대부분 즉시 반환한다.
        /// </summary>
        public void SyncAmbient()
        {
            short ambient = MapLiquidAmbient.ResolveDeciC(Vector3Int.zero);
            if (_ambientSampled &&
                Mathf.Abs(ambient - _lastAmbientDeciC) < MapLiquidConsts.AmbientResampleStepDeciC)
            {
                return;
            }

            _lastAmbientDeciC = ambient;
            _ambientSampled = true;
            MarkAmbientBoundaryDirty();
        }

        /// <summary>
        /// 로드·시드 직후 또는 기온 변화 시 1회 — 대기에 닿은 셀만 dirty로 넣어 평형을 시작한다.
        /// 액체 셀 전체를 훑으므로 매 틱 경로에서 부르지 말 것.
        /// </summary>
        public void MarkAmbientBoundaryDirty()
        {
            foreach (var kv in _overlay.Cells)
            {
                if (kv.Value == null || kv.Value.IsEmpty)
                    continue;

                if (IsAirExposed(kv.Key))
                    _overlay.MarkThermalDirty(kv.Key);
            }
        }

        void ProcessCell(Vector3Int self)
        {
            if (!_overlay.TryGetCell(self, out MapLiquidCell cell) || cell.IsEmpty)
                return;

            int selfTemp = cell.TempDeciC;
            int sumDiff = 0;
            int couplingCount = 0;

            for (int i = 0; i < Neighbors.Length; i++)
            {
                Vector3Int neighbor = self + Neighbors[i];
                if (!_overlay.TryGetCell(neighbor, out MapLiquidCell neighborCell) || neighborCell.IsEmpty)
                    continue;

                sumDiff += neighborCell.TempDeciC - selfTemp;
                couplingCount++;
            }

            if (IsAirExposed(self))
            {
                sumDiff += MapLiquidAmbient.ResolveDeciC(self) - selfTemp;
                couplingCount++;
            }

            if (couplingCount == 0)
                return;

            // 게이트는 relax 후의 delta가 아니라 평균과의 차이에 걸어야 한다 —
            // delta에 걸면 정지 폭이 couplingCount에 비례해 커져(이웃 3개면 1.2 °C) 계약이 흔들린다.
            int meanDiff = sumDiff / couplingCount;
            if (Mathf.Abs(meanDiff) < MapLiquidConsts.MinTempStepDeciC)
                return;

            int delta = meanDiff / MapLiquidConsts.ThermalRelaxDivisor;

            bool wasSolid = cell.IsSolid;
            cell.TempDeciC = (short)Mathf.Clamp(selfTemp + delta, short.MinValue, short.MaxValue);
            bool nowSolid = cell.IsSolid;

            // self는 게이트가 멈출 때까지 계속 relax하고, 이웃은 새 기울기를 받는다.
            _overlay.MarkThermalDirty(self);
            MarkLiquidNeighborsThermalDirty(self);

            if (wasSolid == nowSolid)
                return;

            // 상 교차 — 해동은 흐름을 재개해야 하고, 결빙은 이웃이 새 경계를 다시 평가해야 한다.
            _overlay.MarkDirty(self);
            MarkLiquidNeighborsDirty(self);
            _overlay.RaiseCellChanged(self);
        }

        void MarkLiquidNeighborsThermalDirty(Vector3Int self)
        {
            for (int i = 0; i < Neighbors.Length; i++)
            {
                Vector3Int neighbor = self + Neighbors[i];
                if (_overlay.GetEffectiveMl(neighbor) > 0)
                    _overlay.MarkThermalDirty(neighbor);
            }
        }

        void MarkLiquidNeighborsDirty(Vector3Int self)
        {
            for (int i = 0; i < Neighbors.Length; i++)
            {
                Vector3Int neighbor = self + Neighbors[i];
                if (_overlay.GetEffectiveMl(neighbor) > 0)
                    _overlay.MarkDirty(neighbor);
            }
        }

        /// <summary>위 셀이 비어 있고 그 경계가 열려 있으면 대기와 접한다 — 기온 경계조건이 걸리는 셀.</summary>
        bool IsAirExposed(Vector3Int self)
        {
            Vector3Int above = self + Vector3Int.up;
            if (_overlay.GetEffectiveMl(above) > 0)
                return false;

            return _hub == null || !_hub.CellHasFloor(above.x, above.y, above.z);
        }
    }
}
