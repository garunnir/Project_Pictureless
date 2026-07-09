// ============================================================
// PlayerFloorVisibilityDriver — 플레이어 위치 → 층 가시성 동기화
// ============================================================
using IsoTilemap;
using UnityEngine;

/// <summary>
/// 플레이어 월드 높이·그리드 XZ → <see cref="PlayerFloorVisibilityPolicy"/> →
/// <see cref="IFloorVisibilitySync.SyncFloorVisibility"/>.
/// </summary>
[DefaultExecutionOrder(-98)]
[DisallowMultipleComponent]
public sealed class PlayerFloorVisibilityDriver : MonoBehaviour, IFloorVisibilityDriver
{
    [SerializeField] private CharacterState _playerState;
    [Tooltip("BodyWorldPoint.y에 더할 오프셋(발끝·캡슐 보정).")]
    [SerializeField] private float _heightOffsetWorld;

    [Tooltip("Play 전 Inspector. 끄면 야외 시선상 가림 건물 presentation 숨김을 하지 않습니다.")]
    [SerializeField] private bool _outdoorSightLineBuildingHideEnabled = true;

    private PlayerFloorVisibilityPolicy _policy;
    private IFloorVisibilitySync _visibilitySync;
    private FloorVisibilityContext _lastCtx;
    private bool _hasLastCtx;
    private bool _isActive;
#if UNITY_EDITOR
    private bool _buildingIdLabelsPublished;
#endif

    public void Init(PlayerFloorVisibilityPolicy policy, IFloorVisibilitySync visibilitySync)
    {
        Shutdown();

        _policy = policy;
        _policy.OutdoorSightLineBuildingHideEnabled = _outdoorSightLineBuildingHideEnabled;
        _visibilitySync = visibilitySync;
        _isActive = true;
        ApplyNow();
    }

    public void Shutdown()
    {
#if UNITY_EDITOR
        TileMapBfsDebugOverlay.ClearIndoorOutdoorLayers();
        TileMapBfsDebugOverlay.ClearSightLineLayers();
        TileMapBfsDebugOverlay.ClearBuildingIdLabelLayers();
        _buildingIdLabelsPublished = false;
#endif
        _policy = null;
        _visibilitySync = null;
        _hasLastCtx = false;
        _isActive = false;
    }

    public void ApplyNow()
    {
        if (_policy == null || _visibilitySync == null || _playerState == null)
            return;

        _policy.OutdoorSightLineBuildingHideEnabled = _outdoorSightLineBuildingHideEnabled;

        Vector3 bodyWorld = _playerState.BodyWorldPoint;
        bodyWorld.y += _heightOffsetWorld;

        float playerHeight = bodyWorld.y;
        FloorVisibilityContext ctx = _policy.ResolveContext(playerHeight, bodyWorld);

        if (!_hasLastCtx || !ctx.Equals(_lastCtx))
        {
            _visibilitySync.SyncFloorVisibility(ctx);
            _lastCtx = ctx;
            _hasLastCtx = true;
        }

#if UNITY_EDITOR
        RefreshIndoorOutdoorOverlay(ctx);
        RefreshSightLineBuildingOverlay(bodyWorld, ctx);
        RefreshBuildingIdLabelOverlay();
#endif
    }

#if UNITY_EDITOR
    void RefreshSightLineBuildingOverlay(Vector3 playerWorld, FloorVisibilityContext ctx)
    {
        if (!Config.DebugMode.TileSightLineBuildingOverlay)
        {
            TileMapBfsDebugOverlay.ClearSightLineLayers();
            return;
        }

        if (_policy == null)
            return;

        TileMapBfsDebugOverlay.PublishSightLineBuilding(
            _policy.LastSightLineDebug,
            _policy.CellSize,
            ctx.IsPlayerOutdoor,
            ctx.PlayerBlockingBuildingIds,
            ctx.PlayerBuildingId);
    }

    void RefreshIndoorOutdoorOverlay(FloorVisibilityContext ctx)
    {
        if (!Config.DebugMode.TileIndoorOutdoorOverlay)
        {
            TileMapBfsDebugOverlay.ClearIndoorOutdoorLayers();
            return;
        }

        if (_policy?.MapCache == null)
            return;

        TileMapBfsDebugOverlay.PublishIndoorOutdoorEvaluation(_policy.MapCache, ctx.PlayerFloorCellY);
    }

    void RefreshBuildingIdLabelOverlay()
    {
        if (!Config.DebugMode.TileBuildingIdLabels)
        {
            TileMapBfsDebugOverlay.ClearBuildingIdLabelLayers();
            _buildingIdLabelsPublished = false;
            return;
        }

        if (_buildingIdLabelsPublished || _policy?.MapCache == null)
            return;

        TileMapBfsDebugOverlay.PublishBuildingIdLabels(_policy.MapCache);
        _buildingIdLabelsPublished = true;
    }
#endif

    private void LateUpdate()
    {
        if (!_isActive) return;
        ApplyNow();
    }

    private void OnDestroy() => Shutdown();
}
