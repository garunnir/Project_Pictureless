// ============================================================
// MapPlantInteractable — 식물 오버레이 타겟의 점유셀 식별·단계 외형
// ============================================================

using UnityEngine;
using PlantGrowthStage = global::Garunnir.Runtime.Gameplay.Data.PlantGrowthStage;

namespace IsoTilemap
{
    [DisallowMultipleComponent]
    public sealed class MapPlantInteractable : MonoBehaviour
    {
        MeshFilter _filter;
        MeshRenderer _meshRenderer;
        SpriteRenderer _spriteRenderer;
        int _appliedWorldMinute = int.MinValue;
        bool _appliedFertilized;
        string _appliedSeedItemId;
        bool _hasApplied;

        public Vector3Int Cell { get; private set; }

        void Awake()
        {
            MapPlantVisualHierarchy.CacheFromRoot(
                transform,
                out _filter,
                out _meshRenderer,
                out _spriteRenderer);
            if (_filter == null)
                TryGetComponent(out _filter);
            if (_meshRenderer == null)
                TryGetComponent(out _meshRenderer);
            if (_spriteRenderer == null)
                TryGetComponent(out _spriteRenderer);
        }

        public void BindCell(Vector3Int cell)
        {
            Cell = cell;
            _hasApplied = false;
            ApplyIfNeeded();
        }

        // LateUpdate: one int compare + optional stage apply. No alloc, no GetComponent.
        void LateUpdate() => ApplyIfNeeded();

        void ApplyIfNeeded()
        {
            MapPlantHost host = MapPlantHost.Runtime;
            if (host == null || !host.TryGetPlant(Cell, out PlantCell plant))
                return;

            int worldMinute = MapClockSnapshot.CurrentWorldMinute();
            if (_hasApplied &&
                worldMinute == _appliedWorldMinute &&
                plant.Fertilized == _appliedFertilized &&
                plant.SeedItemId == _appliedSeedItemId)
                return;

            PlantGrowthStage stage = MapPlantOverlayVisual.ResolveStage(in plant);
            MapPlantOverlayVisual.Apply(
                transform,
                _filter,
                _meshRenderer,
                _spriteRenderer,
                Cell,
                host.CellSize,
                stage,
                plant.SeedItemId);
            _appliedWorldMinute = worldMinute;
            _appliedFertilized = plant.Fertilized;
            _appliedSeedItemId = plant.SeedItemId;
            _hasApplied = true;
        }
    }
}
