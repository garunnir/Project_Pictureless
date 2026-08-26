// ============================================================
// PlantTileInteractable — OccupiedCell plant TileView interaction target
// ============================================================

using UnityEngine;

namespace IsoTilemap
{
    [DisallowMultipleComponent]
    public sealed class PlantTileInteractable : MonoBehaviour
    {
        TileView _tileView;

        public Vector3Int Cell
        {
            get
            {
                if (_tileView == null)
                    TryGetComponent(out _tileView);
                return _tileView != null ? _tileView.gridPos : default;
            }
        }

        void Awake() => TryGetComponent(out _tileView);

        void LateUpdate()
        {
            MapPlantHost host = MapPlantHost.Runtime;
            if (host == null || !host.TryGetPlant(Cell, out PlantCell plant))
                return;

            if (!TryGetComponent(out SpriteRenderer spriteRenderer))
                return;

            var stage = MapPlantOverlayVisual.ResolveStage(in plant);
            Sprite sprite = PlantOverlayVisualPresenter.GetStageSprite(stage, plant.SeedItemId);
            if (sprite != null)
                spriteRenderer.sprite = sprite;

            spriteRenderer.color = stage == Garunnir.Runtime.Gameplay.Data.PlantGrowthStage.Withered
                ? MapPlantConsts.OverlayColorWithered
                : Color.white;
        }
    }
}
