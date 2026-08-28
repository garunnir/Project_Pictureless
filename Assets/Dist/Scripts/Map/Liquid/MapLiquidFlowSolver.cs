// ============================================================
// MapLiquidFlowSolver — compressible-water CA equalize + 재귀 수직 탈출
// ============================================================
// docs/map/LIQUID.md SSOT. 순수 반응형(정적 셀 무연산 보증 §2) —
// dirty 큐 pop 외의 순회(전체 overlay 스캔 등)를 절대 하지 않는다.

using UnityEngine;

namespace IsoTilemap
{
    public sealed class MapLiquidFlowSolver
    {
        static readonly Vector3Int[] HorizontalDirs =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
        };

        readonly MapLiquidOverlay _overlay;
        TileMapCacheHub _hub;

        public MapLiquidFlowSolver(MapLiquidOverlay overlay) => _overlay = overlay;

        public void BindMapContext(TileMapCacheHub hub) => _hub = hub;

        /// <summary>
        /// WorldClock.MinuteChanged 1회당 호출. dirty 큐가 비었으면 즉시 반환(비용 0) —
        /// 정지된 바다는 맵 크기와 무관하게 이 호출 자체가 no-op.
        /// </summary>
        public void ProcessDirty(int maxUpdates)
        {
            if (_hub == null)
                return;

            int processed = 0;
            while (processed < maxUpdates && _overlay.TryPopDirty(out Vector3Int cell))
            {
                ProcessCell(cell);
                processed++;
            }
        }

        void ProcessCell(Vector3Int self)
        {
            if (!_overlay.TryGetCell(self, out MapLiquidCell selfCell) || selfCell.IsEmpty)
                return;

            string typeId = selfCell.TypeId;
            int capMl = MapLiquidConsts.DefaultMaxVolumeMl;

            // 1) 중력 — self가 열려 있고(바닥 없음) 아래가 유효하면 stable-state까지 채움
            Vector3Int below = self + Vector3Int.down;
            if (IsVerticalOpen(self) && IsTargetEligible(below))
            {
                int totalMl = _overlay.GetEffectiveMl(self) + _overlay.GetEffectiveMl(below);
                int belowTarget = StableBelowMl(totalMl, capMl, MapLiquidConsts.OverCompressMl);
                int currentBelow = _overlay.GetEffectiveMl(below);
                int moveDown = Mathf.Min(belowTarget - currentBelow, _overlay.GetEffectiveMl(self));
                if (moveDown > MapLiquidConsts.MinFlowMl)
                {
                    _overlay.AddEffectiveMl(below, moveDown, typeId);
                    _overlay.AddEffectiveMl(self, -moveDown, typeId);
                    _overlay.MarkDirty(below);
                }
            }

            if (_overlay.GetEffectiveMl(self) <= 0)
                return; // 전량 낙하 — 더 처리할 것 없음

            // 2) 수평 equalize (diff/4, MinFlowMl 게이트)
            bool anyRoomSideways = false;
            for (int i = 0; i < HorizontalDirs.Length; i++)
            {
                Vector3Int neighbor = self + HorizontalDirs[i];
                if (!IsHorizontalOpen(self, neighbor) || !IsTargetEligible(neighbor))
                    continue;

                int selfMl = _overlay.GetEffectiveMl(self);
                int neighborMl = _overlay.GetEffectiveMl(neighbor);
                int diff = selfMl - neighborMl;
                if (diff <= MapLiquidConsts.MinFlowMl)
                    continue;

                anyRoomSideways = true;
                int flow = Mathf.Clamp(diff / 4, 0, selfMl);
                if (flow <= 0)
                    continue;

                _overlay.AddEffectiveMl(self, -flow, typeId);
                _overlay.AddEffectiveMl(neighbor, flow, typeId);
                _overlay.MarkDirty(neighbor);
            }

            // 3) 옆이 전부 막힘/포화인데 여전히 cap 초과 → 위 칸을 실제로 채움(재귀는 MarkDirty(above)가 담당)
            if (!anyRoomSideways)
            {
                int remainingMl = _overlay.GetEffectiveMl(self);
                if (remainingMl > capMl)
                {
                    Vector3Int above = self + Vector3Int.up;
                    if (IsVerticalOpen(above) && IsTargetEligible(above))
                    {
                        int room = Mathf.Max(0, capMl - _overlay.GetEffectiveMl(above));
                        int overflow = remainingMl - capMl;
                        int moveUp = Mathf.Min(overflow, room);
                        if (moveUp > 0)
                        {
                            _overlay.AddEffectiveMl(above, moveUp, typeId);
                            _overlay.AddEffectiveMl(self, -moveUp, typeId);
                            _overlay.MarkDirty(above);
                        }
                    }
                    // 위도 막힘 = 완전 밀폐. 오픈 지형에서는 사실상 발생하지 않음(컨테이너 맥락은 MlBridge 책임).
                }
            }
        }

        static int StableBelowMl(int totalMl, int capMl, int overCompressMl)
        {
            if (totalMl <= capMl)
                return totalMl;
            if (totalMl < 2 * capMl + overCompressMl)
                return (capMl * capMl + totalMl * overCompressMl) / (capMl + overCompressMl);
            return (totalMl + overCompressMl) / 2;
        }

        /// <summary>맵에 정의된 셀만 액체 시뮬 대상 — 미정의 void로의 무한 확산 차단.</summary>
        bool IsTargetEligible(Vector3Int cell) =>
            _hub.CellHasOccupancy(cell.x, cell.z, cell.y);

        bool IsHorizontalOpen(Vector3Int a, Vector3Int b)
        {
            if (_hub.TryGetEdgeBetween(a, b, out TileData edge))
                return !TileCollisionFlagsUtil.EdgeBlocksPassage(edge);
            return true;
        }

        /// <summary>(하단, upper) 경계가 열려 있는지 — upper 셀에 바닥이 없으면 개방.</summary>
        bool IsVerticalOpen(Vector3Int upper) =>
            !_hub.CellHasFloor(upper.x, upper.y, upper.z);
    }
}
