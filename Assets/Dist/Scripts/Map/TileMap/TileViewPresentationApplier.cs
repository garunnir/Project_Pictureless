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
        private BuildingGroupRegistry _buildingRegistry;

        public TileViewPresentationApplier(ITileViewRegistry registry, TileMapModel model)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public TilePresentationEntryStore Entries => _entries;

        public void ConfigureSightLinePresentation(BuildingGroupRegistry buildingRegistry, int mapMinCellY)
        {
            _buildingRegistry = buildingRegistry;
            _ = mapMinCellY;
        }

        public void ApplySightLineBlockingDelta(in BuildingSightLinePresentationDelta delta)
        {
            if (delta.IsEmpty)
                return;

            IReadOnlyList<int> added = delta.AddedBuildingIds;
            for (int i = 0; i < added.Count; i++)
                SetSightLineHiddenForBuilding(added[i], true);

            IReadOnlyList<int> removed = delta.RemovedBuildingIds;
            for (int i = 0; i < removed.Count; i++)
                SetSightLineHiddenForBuilding(removed[i], false);
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

                float target = PresentationEntryQueries.ResolveCharacterOcclusion(tileId, _entries, _model);
                _characterOcclusionDisplay.TryGetValue(tileId, out float display);
                float newDisplay = OcclusionBlendMath.SmoothTowards(display, target, factor);
                _characterOcclusionDisplay[tileId] = newDisplay;

                if (Mathf.Abs(newDisplay - display) > DisplayEpsilon)
                    view.SetCharacterOcclusion(newDisplay);

                if (target <= DisplayEpsilon && newDisplay <= DisplayEpsilon)
                    _occlusionRemoveScratch.Add(tileId);
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
            {
                _entries.Remove(PresentationConcern.GhostAmount, PresentationSource.Ghost, tileId);
            }

            if (_registry.TryGetView(tileId, out TileView view))
                view.SetGhosted(PresentationEntryQueries.ResolveGhosted(tileId, _entries));
        }

        public void SetSelected(Guid tileId, bool selected)
        {
            _store.SetSelected(tileId, selected);
            if (_registry.TryGetView(tileId, out TileView view))
                view.SetSelected(selected);
        }

        /// <summary>디버그·오버레이: 타일에 관여 중인 entry만 (기본 Query).</summary>
        public IReadOnlyList<TilePresentationEntry> QueryEntriesForTile(Guid tileId) =>
            _entries.Query(PresentationQuery.ForTile(tileId));

        /// <summary>청크 로드·스폰 직후 entry store·선택 상태와 뷰를 맞춥니다.</summary>
        public void SyncPresentationForTile(Guid tileId)
        {
            if (!_registry.TryGetView(tileId, out TileView view))
                return;

            view.SetGhosted(PresentationEntryQueries.ResolveGhosted(tileId, _entries));
            view.SetSelected(_store.IsSelected(tileId));
            view.SetSightLineBuildingHidden(PresentationEntryQueries.ResolveSightLineBuildingHidden(tileId, _entries));

            float target = PresentationEntryQueries.ResolveCharacterOcclusion(tileId, _entries, _model);
            if (target <= DisplayEpsilon)
            {
                _characterOcclusionDisplay.Remove(tileId);
                view.SetCharacterOcclusion(0f);
                return;
            }

            _characterOcclusionDisplay[tileId] = 0f;
            view.SetCharacterOcclusion(0f);
        }

        void SetSightLineHiddenForBuilding(int buildingId, bool hidden)
        {
            if (_buildingRegistry == null || buildingId <= 0)
                return;

            IReadOnlyCollection<Guid> floorIds = _buildingRegistry.GetMinCellYFloorTilesForBuilding(buildingId);

            foreach (Guid tileId in floorIds)
            {
                if (hidden)
                {
                    _entries.Set(
                        PresentationConcern.SightLineBuildingHidden,
                        PresentationSource.BlockingBuildingMinFloor,
                        tileId,
                        1f);
                }
                else
                {
                    _entries.Remove(
                        PresentationConcern.SightLineBuildingHidden,
                        PresentationSource.BlockingBuildingMinFloor,
                        tileId);
                }

                if (_registry.TryGetView(tileId, out TileView view))
                {
                    view.SetSightLineBuildingHidden(
                        PresentationEntryQueries.ResolveSightLineBuildingHidden(tileId, _entries));
                }
            }
        }
    }
}
