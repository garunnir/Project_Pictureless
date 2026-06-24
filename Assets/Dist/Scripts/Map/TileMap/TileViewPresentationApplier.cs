using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    // ============================================================
    // TileViewPresentationApplier — 화면 타일에만 오클루전·고스트·선택 반영
    // ============================================================
    public sealed class TileViewPresentationApplier
    {
        const float DisplayEpsilon = 0.015f;

        private readonly ITileViewRegistry _registry;
        private readonly TileMapModel _model;
        private readonly TilePresentationStore _store = new TilePresentationStore();
        private readonly TilePresentationEntryStore _entries = new TilePresentationEntryStore();
        private readonly Dictionary<Guid, float> _characterOcclusionDisplay = new();
        private readonly List<Guid> _occlusionTickScratch = new();
        private readonly List<Guid> _occlusionRemoveScratch = new();
        private readonly List<Guid> _engagedIdScratch = new();
        private readonly HashSet<Guid> _appliedHidden = new();
        private readonly HashSet<Guid> _appliedSightLineTrace = new();
        private readonly HashSet<Guid> _newHiddenScratch = new();
        private readonly HashSet<Guid> _newTraceScratch = new();
        private readonly HashSet<Guid> _candidateScratch = new();
        private BuildingGroupRegistry _buildingRegistry;
        private PlayerFloorVisibilityPolicy _floorPolicy;
        private FloorVisibilitySyncPlanner _planner;
        private FloorVisibilityHiddenSetComputer _hiddenComputer;
        private FloorVisibilityContext _floorContext;
        private FloorVisibilityContext _lastSyncedCtx;
        private FloorHidePresentationMode _floorHideMode = FloorHidePresentationMode.DisableGameObject;
        private bool _hasFloorContext;
        private bool _hasLastSyncedCtx;

        public TileViewPresentationApplier(ITileViewRegistry registry, TileMapModel model)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public TilePresentationEntryStore Entries => _entries;

        public bool IsFloorVisibilityHidden(Guid tileId) => _appliedHidden.Contains(tileId);

        public bool IsSightLineBuildingTrace(Guid tileId) => _appliedSightLineTrace.Contains(tileId);

        public void ConfigureFloorVisibility(
            PlayerFloorVisibilityPolicy policy,
            BuildingGroupRegistry buildingRegistry,
            TileMapCacheHub hub,
            FloorHidePresentationMode floorHideMode = FloorHidePresentationMode.DisableGameObject)
        {
            _floorPolicy = policy;
            _buildingRegistry = buildingRegistry;
            _planner = hub != null && buildingRegistry != null
                ? new FloorVisibilitySyncPlanner(buildingRegistry, hub)
                : null;
            _hiddenComputer = policy != null
                ? new FloorVisibilityHiddenSetComputer(policy, _model)
                : null;
            SetFloorHidePresentationMode(floorHideMode);
        }

        public void SetFloorHidePresentationMode(FloorHidePresentationMode mode)
        {
            if (_floorHideMode == mode)
                return;

            _floorHideMode = mode;
            ReapplyFloorHiddenPresentation();
        }

        public void ResetFloorVisibilityState()
        {
            _hasFloorContext = false;
            _hasLastSyncedCtx = false;
            _appliedHidden.Clear();
            _appliedSightLineTrace.Clear();
        }

        /// <summary>층 가시성 컨텍스트 변경 시 후보·diff만 반영합니다.</summary>
        public void SyncFloorVisibility(
            in FloorVisibilityContext ctx,
            IMapModelReadOnly model)
        {
            _floorContext = ctx;
            _hasFloorContext = _floorPolicy != null && model != null &&
                               _planner != null && _hiddenComputer != null;

            if (!_hasFloorContext)
                return;

            _planner.BuildCandidateTileIds(
                in ctx,
                in _lastSyncedCtx,
                _hasLastSyncedCtx,
                _appliedHidden,
                _candidateScratch);

            _hiddenComputer.Compute(in ctx, _candidateScratch, _newHiddenScratch);
            ApplyHiddenDiff(_newHiddenScratch, _candidateScratch);
            SyncSightLineTrace(in ctx);

            _lastSyncedCtx = ctx;
            _hasLastSyncedCtx = true;
        }

        void ApplyHiddenDiff(HashSet<Guid> newHidden, HashSet<Guid> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return;

            foreach (Guid tileId in candidates)
            {
                bool shouldHide = newHidden.Contains(tileId);
                if (_appliedHidden.Contains(tileId) == shouldHide)
                    continue;

                if (shouldHide)
                    _appliedHidden.Add(tileId);
                else
                    _appliedHidden.Remove(tileId);

                if (shouldHide)
                    ResetProximityOcclusionPresentation(tileId);

                ApplyResolved(tileId);
            }
        }

        void SyncSightLineTrace(in FloorVisibilityContext ctx)
        {
            _newTraceScratch.Clear();

            if (ctx.IsPlayerOutdoor && _buildingRegistry != null)
            {
                foreach (int buildingId in ctx.PlayerBlockingBuildingIds)
                {
                    IReadOnlyCollection<Guid> traceTiles =
                        _buildingRegistry.GetMinCellYFloorTilesForBuilding(buildingId);
                    foreach (Guid tileId in traceTiles)
                        _newTraceScratch.Add(tileId);
                }
            }

            ApplyTraceDiff(_newTraceScratch);
        }

        void ApplyTraceDiff(HashSet<Guid> newTrace)
        {
            _occlusionRemoveScratch.Clear();
            foreach (Guid tileId in _appliedSightLineTrace)
            {
                if (!newTrace.Contains(tileId))
                    _occlusionRemoveScratch.Add(tileId);
            }

            for (int i = 0; i < _occlusionRemoveScratch.Count; i++)
            {
                Guid tileId = _occlusionRemoveScratch[i];
                _appliedSightLineTrace.Remove(tileId);
                ApplyResolved(tileId);
            }

            foreach (Guid tileId in newTrace)
            {
                if (_appliedSightLineTrace.Contains(tileId))
                    continue;

                _appliedSightLineTrace.Add(tileId);
                ApplyResolved(tileId);
            }
        }

        /// <summary>
        /// SSOT 합성. 구조적 숨김 &gt; 시선 가림 &gt; Ghost &gt; Visible.
        /// 차단 흔적은 별도 오버레이.
        /// </summary>
        public TilePresentationResolved Resolve(Guid tileId)
        {
            bool floorHidden = _appliedHidden.Contains(tileId);
            bool trace = _appliedSightLineTrace.Contains(tileId);
            float occlusion = floorHidden
                ? 0f
                : PresentationEntryQueries.ResolveCharacterOcclusion(tileId, _entries, _model);
            bool ghosted = PresentationEntryQueries.ResolveGhosted(tileId, _entries);
            bool selected = _store.IsSelected(tileId);
            return new TilePresentationResolved(floorHidden, trace, occlusion, ghosted, selected);
        }

        public void ApplyResolved(Guid tileId)
        {
            if (!_registry.TryGetView(tileId, out TileView view))
                return;

            view.ConfigureFloorHidePresentationMode(_floorHideMode);
            view.ApplyResolvedPresentation(Resolve(tileId));
        }

        void ReapplyFloorHiddenPresentation()
        {
            foreach (Guid tileId in _appliedHidden)
                ApplyResolved(tileId);
        }

        /// <summary>BFS 아래벽 블렌드 채널.</summary>
        public void ApplyOcclusionDelta(TileOcclusionPresentationDelta delta) =>
            ApplyCharacterOcclusionDelta(PresentationSource.BfsWallOcclusion, delta);

        /// <summary>카메라↔플레이어 시선 근접 블렌드 채널.</summary>
        public void ApplyProximityBlendDelta(TileOcclusionPresentationDelta delta) =>
            ApplyCharacterOcclusionDelta(PresentationSource.ProximitySightLine, delta);

        void ApplyCharacterOcclusionDelta(PresentationSource source, TileOcclusionPresentationDelta delta)
        {
            if (delta.IsEmpty)
                return;

            ApplyPresentationDelta(delta, source, PresentationConcern.CharacterOcclusion);
        }

        void ApplyPresentationDelta(
            TileOcclusionPresentationDelta delta,
            PresentationSource source,
            PresentationConcern concern)
        {
            _entries.ApplyOcclusionDelta(source, concern, in delta);
        }

        /// <summary>
        /// engaged·페이드 중 타일의 display를 resolved target 쪽으로 보간해 뷰에 반영합니다.
        /// <see cref="CharacterOcclusionDisplayDriver"/>가 매 프레임 호출합니다.
        /// </summary>
        public void TickCharacterOcclusionDisplay(float smoothSpeed, float deltaTime)
        {
            CollectCharacterOcclusionTickTargets(_occlusionTickScratch);
            if (_occlusionTickScratch.Count == 0)
                return;

            float factor = OcclusionBlendMath.ExpSmoothFactor(smoothSpeed, deltaTime);
            _occlusionRemoveScratch.Clear();

            for (int i = 0; i < _occlusionTickScratch.Count; i++)
            {
                Guid tileId = _occlusionTickScratch[i];
                if (!_registry.TryGetView(tileId, out TileView view))
                {
                    _occlusionRemoveScratch.Add(tileId);
                    continue;
                }

                if (IsFloorVisibilityHidden(tileId))
                {
                    FadeOcclusionDisplayTowards(tileId, view, 0f, factor);
                    continue;
                }

                float target = PresentationEntryQueries.ResolveCharacterOcclusion(tileId, _entries, _model);
                _characterOcclusionDisplay.TryGetValue(tileId, out float display);
                float newDisplay = OcclusionBlendMath.SmoothTowards(display, target, factor);
                _characterOcclusionDisplay[tileId] = newDisplay;

                if (target <= DisplayEpsilon && newDisplay <= DisplayEpsilon)
                {
                    _occlusionRemoveScratch.Add(tileId);
                    continue;
                }

                view.SetCharacterOcclusion(newDisplay);
            }

            for (int i = 0; i < _occlusionRemoveScratch.Count; i++)
            {
                Guid tileId = _occlusionRemoveScratch[i];
                _characterOcclusionDisplay.Remove(tileId);

                if (_registry.TryGetView(tileId, out TileView view) &&
                    PresentationEntryQueries.ResolveCharacterOcclusion(tileId, _entries, _model) <= DisplayEpsilon)
                {
                    view.SetCharacterOcclusion(0f);
                }
            }
        }

        /// <summary>셧다운·맵 전환 시 display 상태와 뷰를 초기화합니다.</summary>
        public void ResetCharacterOcclusionDisplay()
        {
            foreach (KeyValuePair<Guid, float> kv in _characterOcclusionDisplay)
            {
                if (_registry.TryGetView(kv.Key, out TileView view))
                    view.SetCharacterOcclusion(0f);
            }

            _characterOcclusionDisplay.Clear();
            _occlusionTickScratch.Clear();
            _occlusionRemoveScratch.Clear();
        }

        void CollectCharacterOcclusionTickTargets(List<Guid> into)
        {
            into.Clear();
            AppendEngagedOcclusionTiles(PresentationSource.ProximitySightLine, into);
            AppendEngagedOcclusionTiles(PresentationSource.BfsWallOcclusion, into);

            foreach (KeyValuePair<Guid, float> kv in _characterOcclusionDisplay)
            {
                if (kv.Value > DisplayEpsilon && !into.Contains(kv.Key))
                    into.Add(kv.Key);
            }
        }

        void AppendEngagedOcclusionTiles(PresentationSource source, List<Guid> into)
        {
            _entries.CollectEngagedTileIds(source, _engagedIdScratch);
            for (int i = 0; i < _engagedIdScratch.Count; i++)
            {
                Guid tileId = _engagedIdScratch[i];
                if (!into.Contains(tileId))
                    into.Add(tileId);
            }
        }

        public void SetGhosted(Guid tileId, bool ghosted)
        {
            if (ghosted)
                _entries.Set(PresentationConcern.GhostAmount, PresentationSource.Ghost, tileId, 1f);
            else
                _entries.Remove(PresentationConcern.GhostAmount, PresentationSource.Ghost, tileId);

            ApplyResolved(tileId);
        }

        public void SetSelected(Guid tileId, bool selected)
        {
            _store.SetSelected(tileId, selected);
            ApplyResolved(tileId);
        }

        /// <summary>디버그·오버레이: 타일에 관여 중인 entry만 (기본 Query).</summary>
        public IReadOnlyList<TilePresentationEntry> QueryEntriesForTile(Guid tileId) =>
            _entries.Query(PresentationQuery.ForTile(tileId));

        /// <summary>청크 로드·스폰 직후 entry store·선택 상태와 뷰를 맞춥니다.</summary>
        public void SyncPresentationForTile(Guid tileId)
        {
            if (!_registry.TryGetView(tileId, out _))
                return;

            if (_hasFloorContext && _model.TryGetTileById(tileId, out TileData tile))
            {
                bool hidden = !_floorPolicy.IsTileVisible(tile, in _floorContext);
                bool trace = ShouldShowSightLineBuildingTrace(tile, in _floorContext, _buildingRegistry);

                if (hidden)
                    _appliedHidden.Add(tileId);
                else
                    _appliedHidden.Remove(tileId);

                if (trace)
                    _appliedSightLineTrace.Add(tileId);
                else
                    _appliedSightLineTrace.Remove(tileId);
            }

            TilePresentationResolved resolved = Resolve(tileId);
            if (!resolved.FloorHidden && resolved.CharacterOcclusion > DisplayEpsilon)
                _characterOcclusionDisplay[tileId] = 0f;
            else
                _characterOcclusionDisplay.Remove(tileId);

            ApplyResolved(tileId);
        }

        static bool ShouldShowSightLineBuildingTrace(
            in TileData tile,
            in FloorVisibilityContext ctx,
            BuildingGroupRegistry buildingRegistry)
        {
            if (!ctx.IsPlayerOutdoor || buildingRegistry == null)
                return false;

            int buildingId = tile.identity.buildingId;
            if (buildingId <= 0 || !ctx.PlayerBlockingBuildingIds.Contains(buildingId))
                return false;

            return TileIdentityUtil.IsFloorTile(tile.identity) &&
                   buildingRegistry.IsBottomFloorTile(buildingId, tile.tileDefId);
        }

        void ResetProximityOcclusionPresentation(Guid tileId)
        {
            _entries.Remove(
                PresentationConcern.CharacterOcclusion,
                PresentationSource.ProximitySightLine,
                tileId);
            _characterOcclusionDisplay.Remove(tileId);
        }

        void FadeOcclusionDisplayTowards(Guid tileId, TileView view, float target, float factor)
        {
            _characterOcclusionDisplay.TryGetValue(tileId, out float display);
            float newDisplay = OcclusionBlendMath.SmoothTowards(display, target, factor);

            if (newDisplay <= DisplayEpsilon)
            {
                _characterOcclusionDisplay.Remove(tileId);
                view.SetCharacterOcclusion(0f);
                return;
            }

            _characterOcclusionDisplay[tileId] = newDisplay;
            view.SetCharacterOcclusion(newDisplay);
        }
    }
}
