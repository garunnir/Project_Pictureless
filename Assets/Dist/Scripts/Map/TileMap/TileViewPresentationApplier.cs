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
        private readonly ITileViewRegistry _registry;
        private readonly TileMapModel _model;
        private readonly TilePresentationStore _store = new TilePresentationStore();
        private readonly TilePresentationEntryStore _entries = new TilePresentationEntryStore();
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
            var touched = new HashSet<Guid>();

            IReadOnlyList<(Guid tileId, float occlusion01)> apply = delta.ApplyEntries;
            for (int i = 0; i < apply.Count; i++)
                touched.Add(apply[i].tileId);

            IReadOnlyList<Guid> clear = delta.ClearIds;
            for (int i = 0; i < clear.Count; i++)
                touched.Add(clear[i]);

            _entries.ApplyOcclusionDelta(source, concern, in delta);

            foreach (Guid tileId in touched)
                PushPresentationToView(tileId);
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
            view.SetCharacterOcclusion(PresentationEntryQueries.ResolveCharacterOcclusion(tileId, _entries, _model));
            view.SetSelected(_store.IsSelected(tileId));
            view.SetSightLineBuildingHidden(PresentationEntryQueries.ResolveSightLineBuildingHidden(tileId, _entries));
        }

        void PushPresentationToView(Guid tileId)
        {
            if (!_registry.TryGetView(tileId, out TileView view))
                return;

            view.SetCharacterOcclusion(PresentationEntryQueries.ResolveCharacterOcclusion(tileId, _entries, _model));
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
