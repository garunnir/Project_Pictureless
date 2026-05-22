// ============================================================
// PlayerFloorVisibilityDriver — 플레이어 위치 → 층 가시성 동기화
// ============================================================
using IsoTilemap;
using UnityEngine;

/// <summary>
/// 플레이어 월드 높이·그리드 XZ → <see cref="PlayerFloorVisibilityPolicy"/> →
/// <see cref="TileMapStreamingVisualizer.SyncFloorVisibility"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerFloorVisibilityDriver : MonoBehaviour
{
    [SerializeField] private CharacterState _playerState;
    [Tooltip("BodyWorldPoint.y에 더할 오프셋(발끝·캡슐 보정).")]
    [SerializeField] private float _heightOffsetWorld;

    private PlayerFloorVisibilityPolicy _policy;
    private TileMapStreamingVisualizer _visualizer;
    private int _lastFloorBand = int.MinValue;
    private int _lastGridX = int.MinValue;
    private int _lastGridZ = int.MinValue;
    private bool _hasLast;

    public void Init(PlayerFloorVisibilityPolicy policy, TileMapStreamingVisualizer visualizer)
    {
        Shutdown();

        _policy = policy;
        _visualizer = visualizer;

        if (_playerState != null)
            _playerState.WorldPoseChanged += OnWorldPoseChanged;
    }

    public void Shutdown()
    {
        if (_playerState != null)
            _playerState.WorldPoseChanged -= OnWorldPoseChanged;

        _policy = null;
        _visualizer = null;
        _hasLast = false;
    }

    public void ApplyNow()
    {
        if (_policy == null || _visualizer == null || _playerState == null)
            return;

        float playerHeight = _playerState.BodyWorldPoint.y + _heightOffsetWorld;
        Vector3Int gridPos = _playerState.GridPos;
        FloorVisibilityContext ctx = _policy.ResolveContext(playerHeight, gridPos.x, gridPos.z);

        if (!_hasLast ||
            ctx.FloorBand != _lastFloorBand ||
            gridPos.x != _lastGridX ||
            gridPos.z != _lastGridZ)
        {
            _visualizer.SyncFloorVisibility(ctx);
            _lastFloorBand = ctx.FloorBand;
            _lastGridX = gridPos.x;
            _lastGridZ = gridPos.z;
            _hasLast = true;
        }
    }

    private void OnWorldPoseChanged(Vector3 _) => ApplyNow();

    private void OnDestroy() => Shutdown();
}
