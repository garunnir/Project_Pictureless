using System;
using System.Collections.Generic;

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
        private BuildingGroupRegistry _buildingRegistry;
        private int _mapMinCellY;
        private OcclusionMode _occlusionMode = OcclusionMode.LegacyCompatible;

        public TileViewPresentationApplier(ITileViewRegistry registry, TileMapModel model)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void ConfigureSightLinePresentation(BuildingGroupRegistry buildingRegistry, int mapMinCellY)
        {
            _buildingRegistry = buildingRegistry;
            _mapMinCellY = mapMinCellY;
        }

        public void SetOcclusionMode(OcclusionMode mode) => _occlusionMode = mode;

        public void ApplySightLineBlockingDelta(in BuildingSightLinePresentationDelta delta)
        {
            if (delta.IsEmpty)
                return;

            IReadOnlyList<int> added = delta.AddedBuildingIds;
            for (int i = 0; i < added.Count; i++)
            {
                int buildingId = added[i];
                _store.SetSightLineHiddenBuilding(buildingId, true);
                ApplySightLineHiddenForBuilding(buildingId, true);
            }

            IReadOnlyList<int> removed = delta.RemovedBuildingIds;
            for (int i = 0; i < removed.Count; i++)
            {
                int buildingId = removed[i];
                _store.SetSightLineHiddenBuilding(buildingId, false);
                ApplySightLineHiddenForBuilding(buildingId, false);
            }
        }

        public void ApplyOcclusionDelta(TileOcclusionPresentationDelta delta)
        {
            if (delta.IsEmpty)
                return;

            IReadOnlyList<(Guid tileId, float occlusion01)> apply = delta.ApplyEntries;
            for (int i = 0; i < apply.Count; i++)
            {
                (Guid tileId, float occlusion01) entry = apply[i];
                if (_registry.TryGetView(entry.tileId, out TileView view))
                    ApplyWallOcclusionByMode(view, entry.occlusion01);
            }

            IReadOnlyList<Guid> clear = delta.ClearIds;
            for (int i = 0; i < clear.Count; i++)
            {
                Guid tileId = clear[i];
                if (_registry.TryGetView(tileId, out TileView view))
                    ApplyWallOcclusionByMode(view, 0f);
            }
        }

        public void SetGhosted(Guid tileId, bool ghosted)
        {
            _store.SetGhosted(tileId, ghosted);
            if (_registry.TryGetView(tileId, out TileView view))
                view.SetGhosted(ghosted);
        }

        public void SetSelected(Guid tileId, bool selected)
        {
            _store.SetSelected(tileId, selected);
            if (_registry.TryGetView(tileId, out TileView view))
                view.SetSelected(selected);
        }

        /// <summary>청크 로드·스폰 직후 모델 캐시·store와 뷰를 맞춥니다.</summary>
        public void SyncPresentationForTile(Guid tileId)
        {
            if (!_registry.TryGetView(tileId, out TileView view))
                return;

            float occ = 0f;
            if (_model.TryGetTileOcclusionPresentation(tileId, out float cached))
                occ = cached;
            view.SetGhosted(_store.IsGhosted(tileId));
            ApplyWallOcclusionByMode(view, occ);
            view.SetSelected(_store.IsSelected(tileId));
            view.SetSightLineBuildingHidden(ResolveSightLineHiddenForTile(tileId));
        }

        void ApplyWallOcclusionByMode(TileView view, float occlusion01)
        {
            if (view == null)
                return;

            view.ApplyWallOcclusionMode(occlusion01, _occlusionMode);
        }

        bool ResolveSightLineHiddenForTile(Guid tileId)
        {
            if (_buildingRegistry == null ||
                !_model.TryGetTileById(tileId, out TileData tile))
                return false;

            if ((TileView.TileType)tile.identity.tileType != TileView.TileType.Floor)
                return false;

            if (tile.identity.GridPos.y != _mapMinCellY)
                return false;

            return _store.IsSightLineHiddenBuilding(tile.identity.buildingId);
        }

        void ApplySightLineHiddenForBuilding(int buildingId, bool hidden)
        {
            if (_buildingRegistry == null)
                return;

            IReadOnlyCollection<Guid> floorIds = _buildingRegistry.GetMinCellYFloorTilesForBuilding(buildingId);
            foreach (Guid tileId in floorIds)
            {
                if (_registry.TryGetView(tileId, out TileView view))
                    view.SetSightLineBuildingHidden(hidden);
            }
        }
    }
}
