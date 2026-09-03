// ============================================================
// MapTopologyDepenetration — 그리드 통행 불가 진입 시 역방향 topology 탈출 push
// ============================================================
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// footprint 볼륨이 벽 셀(<see cref="IMapTopologyQuery.CellHasSolidWall"/>)에 침범하면 이동과 무관하게 탈출 push를 적용합니다.
    /// non-wall → wall 진입 순간의 그리드 델타 반대 방향을 캐시합니다.
    /// </summary>
    public sealed class MapTopologyDepenetration
    {
        const float Epsilon = 1e-6f;

        static readonly Vector3Int[] CardinalGridDirs =
        {
            Vector3Int.right, Vector3Int.back, Vector3Int.left, Vector3Int.forward
        };

        static readonly Vector3Int[] ProbeGridDirs =
        {
            Vector3Int.right, Vector3Int.back, Vector3Int.left, Vector3Int.forward,
            Vector3Int.up, Vector3Int.down
        };

        readonly IMapTopologyQuery _query;
        readonly MapTopologyCollisionResolver _resolver;
        readonly float _cellSize;

        public readonly struct PushOutResult
        {
            public readonly bool WasBlocking;
            public readonly bool StillBlocking;

            public PushOutResult(bool wasBlocking, bool stillBlocking)
            {
                WasBlocking = wasBlocking;
                StillBlocking = stillBlocking;
            }

            public static PushOutResult None => new PushOutResult(false, false);
        }

        /// <summary>플레이어(유닛)마다 하나씩 유지합니다.</summary>
        public struct Tracker
        {
            public Vector3Int LastFeetGrid;
            public bool HasLastFeetGrid;
            public Vector3Int EscapeGridDir;
            public bool HasEscapeDir;
        }

        public MapTopologyDepenetration(IMapTopologyQuery query, MapTopologyCollisionResolver resolver)
        {
            _query = query;
            _resolver = resolver;
            _cellSize = query.CellSize > 0f ? query.CellSize : 1f;
        }

        /// <summary>끼임 = 벽 셀 침범만. 공중(!Floor)은 낙하·계단 정상 상태.</summary>
        public bool IsGridBlocked(Vector3Int cell) =>
            _query.CellHasSolidWall(cell.x, cell.z, cell.y);

        public bool IsFootprintBlocking(Vector3 feetWorld, Vector3Int footprint)
        {
            var feetCell = TileHelper.ConvertWorldToGrid(feetWorld, _cellSize);
            return IsFootprintBlocking(feetCell, footprint);
        }

        public bool IsFootprintBlocking(Vector3Int feetCell, Vector3Int footprint) =>
            MapTopologyCollisionResolver.FootprintVolumeBlocks(_query, feetCell, footprint);

        /// <summary>
        /// 그리드 끼임을 감지·탈출합니다. <paramref name="tracker"/>는 호출자가 유닛별로 보관합니다.
        /// </summary>
        public PushOutResult TryResolveGridStuck(
            ref Vector3 bodyWorld,
            float feetOffset,
            ref MapCollisionGrid.FeetCell feet,
            ref Tracker tracker,
            Vector3Int footprint,
            float pushSpeed,
            int maxIterations,
            float deltaTime)
        {
            footprint = MapTopologyCollisionResolver.ClampFootprint(footprint);
            Vector3Int currentFeetGrid = MapCollisionGrid.ToGrid(feet);
            bool wasBlocking = IsFootprintBlocking(currentFeetGrid, footprint);

            if (!wasBlocking)
            {
                tracker.HasEscapeDir = false;
                tracker.LastFeetGrid = currentFeetGrid;
                tracker.HasLastFeetGrid = true;
                return PushOutResult.None;
            }

            CaptureEscapeDirection(ref tracker, currentFeetGrid, footprint);

            float minStep = _cellSize * 0.55f;
            float stepCap = Mathf.Max(pushSpeed * deltaTime, minStep);
            int iterations = Mathf.Max(1, maxIterations);
            bool stillBlocking = true;

            for (int i = 0; i < iterations; i++)
            {
                if (!IsFootprintBlocking(currentFeetGrid, footprint))
                {
                    tracker.HasEscapeDir = false;
                    tracker.LastFeetGrid = currentFeetGrid;
                    tracker.HasLastFeetGrid = true;
                    return new PushOutResult(true, false);
                }

                if (!TryComputePush(
                        feet.FeetWorld,
                        currentFeetGrid,
                        tracker,
                        footprint,
                        stepCap,
                        out Vector3 push))
                    break;

                bodyWorld += push;
                feet = MapCollisionGrid.ResolveFeetCell(bodyWorld, feetOffset, _cellSize);
                currentFeetGrid = MapCollisionGrid.ToGrid(feet);
                stillBlocking = IsFootprintBlocking(currentFeetGrid, footprint);
            }

            tracker.LastFeetGrid = currentFeetGrid;
            tracker.HasLastFeetGrid = true;
            if (!stillBlocking)
                tracker.HasEscapeDir = false;

            return new PushOutResult(true, stillBlocking);
        }

        /// <summary>non-wall → wall footprint 진입 시 탈출 방향을 캐시합니다.</summary>
        public void CaptureGridTransition(
            ref Tracker tracker,
            Vector3Int fromFeetGrid,
            Vector3Int toFeetGrid,
            Vector3Int footprint)
        {
            if (IsFootprintBlocking(fromFeetGrid, footprint) ||
                !IsFootprintBlocking(toFeetGrid, footprint))
                return;

            tracker.EscapeGridDir = fromFeetGrid - toFeetGrid;
            tracker.HasEscapeDir = tracker.EscapeGridDir != Vector3Int.zero;
        }

        void CaptureEscapeDirection(ref Tracker tracker, Vector3Int currentFeetGrid, Vector3Int footprint)
        {
            if (!tracker.HasLastFeetGrid)
                return;

            CaptureGridTransition(ref tracker, tracker.LastFeetGrid, currentFeetGrid, footprint);
        }

        bool TryComputePush(
            Vector3 feetWorld,
            Vector3Int currentFeetGrid,
            Tracker tracker,
            Vector3Int footprint,
            float stepCap,
            out Vector3 push)
        {
            push = Vector3.zero;

            if (tracker.HasEscapeDir)
            {
                push = PushTowardGridDir(feetWorld, currentFeetGrid, tracker.EscapeGridDir, footprint, stepCap);
                if (push.sqrMagnitude > Epsilon)
                    return true;
            }

            push = ProbeTowardNearestWalkable(feetWorld, currentFeetGrid, footprint, stepCap);
            return push.sqrMagnitude > Epsilon;
        }

        Vector3 PushTowardGridDir(
            Vector3 feetWorld,
            Vector3Int currentFeetGrid,
            Vector3Int escapeGridDir,
            Vector3Int footprint,
            float stepCap)
        {
            Vector3Int targetFeetGrid = currentFeetGrid + escapeGridDir;
            if (IsFootprintBlocking(targetFeetGrid, footprint))
                return Vector3.zero;

            return MoveTowardGridCenter(feetWorld, targetFeetGrid, footprint, stepCap);
        }

        Vector3 ProbeTowardNearestWalkable(
            Vector3 feetWorld,
            Vector3Int currentFeetGrid,
            Vector3Int footprint,
            float stepCap)
        {
            Vector3 best = Vector3.zero;
            float bestSqr = Epsilon;

            for (int i = 0; i < ProbeGridDirs.Length; i++)
            {
                Vector3Int neighborFeet = currentFeetGrid + ProbeGridDirs[i];
                if (IsFootprintBlocking(neighborFeet, footprint))
                    continue;

                Vector3 candidate = MoveTowardGridCenter(feetWorld, neighborFeet, footprint, stepCap);
                if (candidate.sqrMagnitude > bestSqr)
                {
                    bestSqr = candidate.sqrMagnitude;
                    best = candidate;
                }
            }

            return best;
        }

        Vector3 MoveTowardGridCenter(
            Vector3 feetWorld,
            Vector3Int targetFeetGrid,
            Vector3Int footprint,
            float stepCap)
        {
            Vector3 targetFeet = TileHelper.ConvertGridToWorldPos(targetFeetGrid, _cellSize);
            Vector3 flat = new Vector3(targetFeet.x - feetWorld.x, 0f, targetFeet.z - feetWorld.z);
            if (flat.sqrMagnitude > Epsilon)
            {
                float dist = flat.magnitude;
                float step = Mathf.Min(stepCap, dist);
                Vector3 wish = flat / dist * step;
                Vector3 clamped = _resolver.ClampHorizontal(feetWorld, wish, footprint);
                return new Vector3(clamped.x, 0f, clamped.z);
            }

            float yDelta = targetFeet.y - feetWorld.y;
            if (Mathf.Abs(yDelta) <= Epsilon)
                return Vector3.zero;

            float yStep = Mathf.Min(stepCap, Mathf.Abs(yDelta));
            return new Vector3(0f, Mathf.Sign(yDelta) * yStep, 0f);
        }
    }
}
