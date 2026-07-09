// ============================================================
// SightLineProximityBlendDriver — 카메라↔플레이어 시선 근접 블렌드 갱신
// ============================================================
using System;
using System.Collections.Generic;
using IsoTilemap;
using UnityEngine;

/// <summary>
/// 플레이어·카메라 위치로 <see cref="ProximitySightLineBlendPipeline"/>을 실행하고
/// <see cref="TileViewPresentationApplier"/>에 반영합니다.
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class SightLineProximityBlendDriver : MonoBehaviour, IProximityBlendDriver
{
    [SerializeField] private CharacterState _playerState;
    [SerializeField] private TileMapManager _tileMapManager;

    [SerializeField] private SightLineBlendSettings _blendSettings = SightLineBlendSettings.DefaultUnity;

    ProximitySightLineBlendPipeline _pipeline;
    readonly Dictionary<Guid, float> _previousScratch = new();
    readonly List<Guid> _clearScratch = new();
    readonly HashSet<int> _blockingScratch = new();
    readonly HashSet<Vector3Int> _blockingCellsScratch = new();
    bool _isActive;

    public void Init(
        TileMapCacheHub hub,
        TileViewPresentationApplier applier,
        PlayerFloorVisibilityPolicy policy,
        System.Func<Camera> resolveCamera)
    {
        Shutdown();
        if (hub == null || applier == null || policy == null || resolveCamera == null)
            return;

        _pipeline = new ProximitySightLineBlendPipeline(hub);
        _presentationApplier = applier;
        _policy = policy;
        _resolveCamera = resolveCamera;
        _isActive = true;
        ApplyNow();
    }

    TileViewPresentationApplier _presentationApplier;
    PlayerFloorVisibilityPolicy _policy;
    System.Func<Camera> _resolveCamera;

    public void Shutdown()
    {
        if (_isActive && _presentationApplier != null)
        {
            TilePresentationEntryStore entries = _presentationApplier.Entries;
            entries.CollectEngagedTileIds(PresentationSource.ProximitySightLine, _clearScratch);
            if (_clearScratch.Count > 0)
            {
                _presentationApplier.ApplyProximityBlendDelta(
                    new TileOcclusionPresentationDelta(
                        System.Array.Empty<(System.Guid, float)>(),
                        _clearScratch));
            }

            entries.SetSourceEngaged(PresentationSource.ProximitySightLine, false);
        }

        _pipeline = null;
        _presentationApplier = null;
        _policy = null;
        _resolveCamera = null;
        _previousScratch.Clear();
        _clearScratch.Clear();
        _isActive = false;
    }

    public void ApplyNow()
    {
        if (!_isActive || _playerState == null || _pipeline == null || _presentationApplier == null)
            return;

        SyncCellSizeFromGrid();

        Vector3 playerWorld = _playerState.IsAiming
            ? _playerState.AimWorldPoint
            : _playerState.BodyWorldPoint;

        Camera cam = _resolveCamera?.Invoke();
        if (cam == null)
            return;

        Vector3 cameraWorld = cam.transform.position;

        float playerHeight = playerWorld.y;
        FloorVisibilityContext ctx = _policy.ResolveContext(playerHeight, playerWorld);
        Vector3Int playerCell = _policy.ResolvePlayerOccupiedCell(playerHeight, playerWorld);

        TilePresentationEntryStore entries = _presentationApplier.Entries;
        entries.CopyScalarsForSource(
            PresentationSource.ProximitySightLine,
            PresentationConcern.CharacterOcclusion,
            _previousScratch);

        ProximityBlendEvaluationResult result = _pipeline.Evaluate(
            cameraWorld,
            playerWorld,
            playerCell,
            ctx.PlayerFloorCellY,
            ctx.IsPlayerOutdoor,
            _blendSettings,
            _previousScratch);

        TileOcclusionPresentationDelta delta = result.Delta;
        ProximityBlendEvaluationSnapshot snapshot = result.Snapshot;

        ProximityBuildingHideAddon.CollectBlockingBuildingIds(
            in snapshot,
            ctx.PlayerBuildingId,
            ctx.IsPlayerOutdoor,
            _policy.OutdoorSightLineBuildingHideEnabled,
            _blockingScratch,
            _blockingCellsScratch);

        SightLineBuildingDebugSnapshot debug = ProximityBuildingHideAddon.BuildDebugSnapshot(
            in snapshot,
            _blockingScratch,
            _blockingCellsScratch,
            ctx.IsPlayerOutdoor);
        _policy.SetProximityBlockingBuildingIds(_blockingScratch, debug);

        if (!delta.IsEmpty)
            _presentationApplier.ApplyProximityBlendDelta(delta);
    }

    void SyncCellSizeFromGrid()
    {
        if (_tileMapManager?.WorldGrid == null)
            return;

        SightLineBlendSettings s = _blendSettings;
        s.CellSize = _tileMapManager.WorldGrid.CellSize;
        _blendSettings = s;
    }

    void LateUpdate()
    {
        if (!_isActive)
            return;

        ApplyNow();
    }

    void OnDestroy() => Shutdown();

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_playerState == null)
            _playerState = GetComponent<CharacterState>();
    }
#endif
}
