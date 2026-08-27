// ============================================================
// MapHearingPingHost — 맵 청각 핑 런타임 호스트 (Overlay + Renderer)
// ============================================================

using UnityEngine;

namespace IsoTilemap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapHearingPingRenderer))]
    public sealed class MapHearingPingHost : MonoBehaviour
    {
        [SerializeField] MapHearingPingRenderer _renderer;

        readonly MapHearingPingOverlay _overlay = new();
        float _cellSize = 1f;

        public MapHearingPingOverlay Overlay => _overlay;
        public float CellSize => _cellSize;

        void Awake()
        {
            _renderer ??= GetComponent<MapHearingPingRenderer>();
            _renderer?.Bind(_overlay, _cellSize);
        }

        public void BindMapContext(float cellSize)
        {
            _cellSize = Mathf.Max(1e-4f, cellSize);
            _renderer ??= GetComponent<MapHearingPingRenderer>();
            _renderer?.Bind(_overlay, _cellSize);
        }

        public void ConfigureDraw(float quadSizeMeters, float yOffsetMeters, float maxAlpha)
        {
            _renderer ??= GetComponent<MapHearingPingRenderer>();
            _renderer?.ConfigureDraw(quadSizeMeters, yOffsetMeters, maxAlpha);
        }
    }
}
