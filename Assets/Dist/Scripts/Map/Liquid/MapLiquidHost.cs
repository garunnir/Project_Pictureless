// ============================================================
// MapLiquidHost — 맵 액체 런타임 호스트 (로드·세이브·틱 API)
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    [DisallowMultipleComponent]
    public sealed class MapLiquidHost : MonoBehaviour
    {
        /// <summary>씬에 바인딩된 액체 호스트.</summary>
        public static MapLiquidHost Runtime { get; private set; }

        readonly MapLiquidOverlay _overlay = new();
        MapLiquidFlowSolver _solver;
        TileMapCacheHub _hub;
        float _cellSize = 1f;
        bool _clockSubscribed;

        public MapLiquidOverlay Overlay => _overlay;
        public float CellSize => _cellSize;

        void Awake()
        {
            Runtime = this;
            _solver = new MapLiquidFlowSolver(_overlay);
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
            if (_clockSubscribed || WorldClock.Instance == null)
                return;

            WorldClock.Instance.MinuteChanged += OnWorldMinuteChanged;
            _clockSubscribed = true;
        }

        void UnsubscribeClock()
        {
            if (!_clockSubscribed)
                return;

            if (WorldClock.Instance != null)
                WorldClock.Instance.MinuteChanged -= OnWorldMinuteChanged;
            _clockSubscribed = false;
        }

        void OnWorldMinuteChanged() => _solver.ProcessDirty(MapLiquidConsts.MaxUpdatesPerTick);

        public void BindMapContext(TileMapCacheHub hub, float cellSize)
        {
            _hub = hub;
            _cellSize = Mathf.Max(1e-4f, cellSize);
            _solver.BindMapContext(hub);
        }

        /// <summary>
        /// dto.hasLiquidSnapshot이 true면 저장된 상태만 신뢰(플레이어가 비운 웅덩이 등)하고 재시드하지 않는다.
        /// false(레거시/최초 로드)면 SHALLOW_WATER/DEEP_WATER 바닥 태그로 1회 시드한다.
        /// </summary>
        public void LoadFromDto(MapSaveJsonDto dto)
        {
            _overlay.Clear();

            if (dto != null && dto.hasLiquidSnapshot)
            {
                _overlay.LoadFromDto(dto.liquidCells);
                return;
            }

            _overlay.SeedFromTileFlags(_hub);
        }

        public void WriteToDto(MapSaveJsonDto dto)
        {
            if (dto == null)
                return;

            dto.liquidCells ??= new List<MapLiquidCellSaveData>();
            _overlay.WriteToDto(dto.liquidCells);
            dto.hasLiquidSnapshot = true;
        }
    }
}
