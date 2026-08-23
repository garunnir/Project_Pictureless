// ============================================================
// MapBloodHost — 맵 혈흔 런타임 호스트 (로드·세이브·스탬프 API)
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapBloodStainRenderer))]
    public sealed class MapBloodHost : MonoBehaviour
    {
        /// <summary>씬에 바인딩된 혈흔 호스트. writers가 사용.</summary>
        public static MapBloodHost Runtime { get; private set; }

        [SerializeField] MapBloodStainRenderer _renderer;

        readonly MapBloodOverlay _overlay = new();
        TileMapCacheHub _hub;
        float _cellSize = 1f;

        public MapBloodOverlay Overlay => _overlay;
        public float CellSize => _cellSize;

        void Awake()
        {
            _renderer ??= GetComponent<MapBloodStainRenderer>();
            _renderer?.Bind(_overlay);
            Runtime = this;
        }

        void OnDestroy()
        {
            if (ReferenceEquals(Runtime, this))
                Runtime = null;
        }

        public void BindMapContext(TileMapCacheHub hub, float cellSize)
        {
            _hub = hub;
            _cellSize = Mathf.Max(1e-4f, cellSize);
        }

        public void LoadFromDto(MapSaveJsonDto dto)
        {
            if (dto?.bloodStamps == null)
            {
                _overlay.Clear();
                return;
            }

            _overlay.LoadFromDto(dto.bloodStamps);
        }

        public void WriteToDto(MapSaveJsonDto dto)
        {
            if (dto == null)
                return;
            dto.bloodStamps ??= new List<BloodStampSaveData>();
            _overlay.WriteToDto(dto.bloodStamps);
        }

        public void AddStamp(Vector3 worldPos, float yaw, float scale, float alpha)
        {
            _overlay.AddStamp(worldPos, yaw, scale, alpha, _hub, _cellSize);
        }

        public void Spray(
            Vector3 origin,
            Vector3 direction,
            int count,
            float coneHalfRad,
            float minDist,
            float maxDist,
            float groundBiasY,
            float scale,
            float alpha)
        {
            _overlay.Spray(
                origin,
                direction,
                count,
                coneHalfRad,
                minDist,
                maxDist,
                groundBiasY,
                scale,
                alpha,
                _hub,
                _cellSize);
        }

        public int ClearCell(Vector3Int cell) => _overlay.ClearCell(cell);
    }
}
