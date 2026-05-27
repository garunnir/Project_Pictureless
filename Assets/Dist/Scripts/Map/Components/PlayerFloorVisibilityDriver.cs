// ============================================================
// PlayerFloorVisibilityDriver — 플레이어 위치 → 층 가시성 동기화
// ============================================================
using IsoTilemap;
using UnityEngine;

/// <summary>
/// 플레이어 월드 높이·그리드 XZ → <see cref="PlayerFloorVisibilityPolicy"/> →
/// <see cref="TileMapStreamingVisualizer.SyncFloorVisibility"/>.
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class PlayerFloorVisibilityDriver : MonoBehaviour
{
    [SerializeField] private CharacterState _playerState;
    [Tooltip("BodyWorldPoint.y에 더할 오프셋(발끝·캡슐 보정).")]
    [SerializeField] private float _heightOffsetWorld;

    [Tooltip("Play 전 Inspector. 끄면 야외 시선상 가림 건물 숨김(벽 despawn)을 하지 않습니다.")]
    [SerializeField] private bool _outdoorSightLineBuildingHideEnabled = true;

    private PlayerFloorVisibilityPolicy _policy;
    private TileMapStreamingVisualizer _visualizer;
    private FloorVisibilityContext _lastCtx;
    private bool _hasLastCtx;
    private bool _isActive;

    public void Init(PlayerFloorVisibilityPolicy policy, TileMapStreamingVisualizer visualizer)
    {
        Shutdown();

        _policy = policy;
        _policy.OutdoorSightLineBuildingHideEnabled = _outdoorSightLineBuildingHideEnabled;
        _visualizer = visualizer;
        _isActive = true;
        ApplyNow(); // 최초 1회 동기화 
    }

    public void Shutdown()
    {
        _policy = null;
        _visualizer = null;
        _hasLastCtx = false;
        _isActive = false;
    }

    public void ApplyNow()
    {
        if (_policy == null || _visualizer == null || _playerState == null)
            return;

        _policy.OutdoorSightLineBuildingHideEnabled = _outdoorSightLineBuildingHideEnabled;

        Vector3 bodyWorld = _playerState.BodyWorldPoint;
        bodyWorld.y += _heightOffsetWorld;

        float playerHeight = bodyWorld.y;
        Vector3Int gridPos = _playerState.GridPos;
        FloorVisibilityContext ctx = _policy.ResolveContext(
            playerHeight, gridPos.x, gridPos.z, bodyWorld);

        if (!_hasLastCtx || !ctx.Equals(_lastCtx))
        {
            _visualizer.SyncFloorVisibility(ctx);
            _lastCtx = ctx;
            _hasLastCtx = true;
        }
    }

    private void LateUpdate()
    {
        if (!_isActive) return;
        ApplyNow();
    }

    private void OnDestroy() => Shutdown();
}
