// ============================================================
// MapLiquidHost — 맵 액체 런타임 호스트 (로드·세이브·틱 API)
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapLiquidSurfaceRenderer))]
    public sealed class MapLiquidHost : MonoBehaviour
    {
        /// <summary>씬에 바인딩된 액체 호스트.</summary>
        public static MapLiquidHost Runtime { get; private set; }

        [SerializeField] MapLiquidSurfaceRenderer _surfaceRenderer;

        readonly MapLiquidOverlay _overlay = new();
        MapLiquidFlowSolver _solver;
        MapLiquidThermalSolver _thermalSolver;
        float _cellSize = 1f;
        bool _clockSubscribed;

        public MapLiquidOverlay Overlay => _overlay;
        public float CellSize => _cellSize;

        void Awake()
        {
            Runtime = this;
            _solver = new MapLiquidFlowSolver(_overlay);
            _thermalSolver = new MapLiquidThermalSolver(_overlay);
            ResolveSurfaceRenderer()?.Bind(_overlay);
        }

        /// <summary>Missing 참조는 Unity의 fake-null이라 ??=로 걸러지지 않는다 — == 비교로 한 번 더 태운다.</summary>
        MapLiquidSurfaceRenderer ResolveSurfaceRenderer()
        {
            if (_surfaceRenderer == null)
                _surfaceRenderer = GetComponent<MapLiquidSurfaceRenderer>();

            if (_surfaceRenderer == null)
            {
                Debug.LogError(
                    "[MapLiquidHost] MapLiquidSurfaceRenderer가 없어 수면이 그려지지 않습니다.",
                    this);
                return null;
            }

            return _surfaceRenderer;
        }

        void OnEnable() => TrySubscribeClock();

        void OnDisable() => UnsubscribeClock();

        void OnDestroy()
        {
            UnsubscribeClock();
            if (ReferenceEquals(Runtime, this))
                Runtime = null;
        }

        void Update()
        {
            // WorldClock이 아직 씬에 없던 시점(OnEnable)에 구독 실패했을 수 있어 지연 재시도.
            if (!_clockSubscribed)
                TrySubscribeClock();
        }

        void TrySubscribeClock()
        {
            if (_clockSubscribed)
                return;

            if (MapClockSnapshot.TrySubscribeMinuteChanged?.Invoke(OnWorldMinuteChanged) != true)
                return;

            _clockSubscribed = true;
        }

        void UnsubscribeClock()
        {
            if (!_clockSubscribed)
                return;

            MapClockSnapshot.UnsubscribeMinuteChanged?.Invoke(OnWorldMinuteChanged);
            _clockSubscribed = false;
        }

        void OnWorldMinuteChanged()
        {
            // 기온 변화 감지 → 열 확산 → 흐름. 상 교차가 flow dirty를 넣으므로 열이 먼저다.
            _thermalSolver.SyncAmbient();
            _thermalSolver.ProcessDirty(MapLiquidConsts.MaxThermalUpdatesPerTick);
            _solver.ProcessDirty(MapLiquidConsts.MaxUpdatesPerTick);
        }

        public void BindMapContext(TileMapCacheHub hub, float cellSize)
        {
            _cellSize = Mathf.Max(1e-4f, cellSize);
            _solver.BindMapContext(hub);
            _thermalSolver.BindMapContext(hub);
        }

        /// <summary>
        /// 수면 렌더 컨텍스트. 청크 분할 SSOT는 <see cref="TileMapChunkStreamer.ChunkSize"/>이며,
        /// 스트리밍이 없으면 <paramref name="chunkSize"/>에 0을 넘겨 렌더러 폴백을 쓰게 한다.
        /// LoadFromDto보다 **먼저** 호출해야 로드 시 발생하는 BulkChanged 통지를 렌더러가 받는다.
        /// </summary>
        public void BindRenderContext(int chunkSize, TileMapStreamingVisualizer streaming)
        {
            ResolveSurfaceRenderer()?.BindMapContext(_cellSize, chunkSize, streaming);
        }

        /// <summary>
        /// dto.hasLiquidSnapshot이 true면 저장된 상태만 신뢰(플레이어가 비운 웅덩이 등)하고 재시드하지 않는다.
        /// false(레거시/최초 로드)면 물 저작 면으로 1회 시드한다.
        /// </summary>
        public void LoadFromDto(MapSaveJsonDto dto)
        {
            if (dto != null && dto.hasLiquidSnapshot)
                _overlay.LoadFromDto(dto.liquidCells, dto.hasLiquidTemperature);
            else
            {
                _overlay.Clear();
                _overlay.SeedFromAuthoringFaces(dto?.liquidAuthoringFaces);
            }

            // 시드는 dirty를 넣지 않는다. 대기에 닿은 셀만 한 번 깨워 기온과의 평형을 시작한다.
            _thermalSolver.MarkAmbientBoundaryDirty();
        }

        public void WriteToDto(MapSaveJsonDto dto)
        {
            if (dto == null)
                return;

            dto.liquidCells ??= new List<MapLiquidCellSaveData>();
            _overlay.WriteToDto(dto.liquidCells);
            dto.hasLiquidSnapshot = true;
            dto.hasLiquidTemperature = true;
        }
    }
}
