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

        public TileViewPresentationApplier(ITileViewRegistry registry, TileMapModel model)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _model = model ?? throw new ArgumentNullException(nameof(model));
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
                    view.SetCharacterOcclusion(entry.occlusion01);
            }

            IReadOnlyList<Guid> clear = delta.ClearIds;
            for (int i = 0; i < clear.Count; i++)
            {
                Guid tileId = clear[i];
                if (_registry.TryGetView(tileId, out TileView view))
                    view.SetCharacterOcclusion(0f);
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
            view.SetCharacterOcclusion(occ);
            view.SetSelected(_store.IsSelected(tileId));
        }
    }
}
