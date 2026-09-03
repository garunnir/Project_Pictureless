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

    public void SetPlayerState(CharacterState playerState) => _playerState = playerState;

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
        Vector3Int feetCell = _playerState.GridPos;
        Vector3Int footprint = _playerState.GridFootprint;

        FloorVisibilityContext ctx = _policy.ResolveContext(playerHeight, playerWorld, feetCell, footprint);
        Vector3Int playerCell = feetCell;

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
        ExcludeFootprintOccupiedBlocking(
            _policy,
            in snapshot,
            feetCell,
            footprint,
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

    static void ExcludeFootprintOccupiedBlocking(
        PlayerFloorVisibilityPolicy policy,
        in ProximityBlendEvaluationSnapshot snapshot,
        Vector3Int feetCell,
        Vector3Int footprint,
        HashSet<int> blockingBuildingIds,
        HashSet<Vector3Int> blockingCellsScratch)
    {
        IReadOnlyList<ProximityEvaluatedHit> hits = snapshot.EvaluatedHits;
        if (hits == null || hits.Count == 0)
            return;

        var footprintOnlyBuildingIds = new HashSet<int>();
        var outsideFootprintBuildingIds = new HashSet<int>();
        for (int i = 0; i < hits.Count; i++)
        {
            ProximityEvaluatedHit hit = hits[i];
            int buildingId = hit.Tile.identity.buildingId;
            if (buildingId <= 0)
                continue;

            if (policy.IsPlayerOccupiedCell(hit.OccupiedCell, feetCell, footprint))
                footprintOnlyBuildingIds.Add(buildingId);
            else
                outsideFootprintBuildingIds.Add(buildingId);
        }

        foreach (int buildingId in footprintOnlyBuildingIds)
        {
            if (!outsideFootprintBuildingIds.Contains(buildingId))
                blockingBuildingIds.Remove(buildingId);
        }

        if (blockingCellsScratch == null || blockingCellsScratch.Count == 0)
            return;

        blockingCellsScratch.RemoveWhere(cell => policy.IsPlayerOccupiedCell(cell, feetCell, footprint));
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
