// ============================================================
// PlantOverlaySpriteCatalog — PlantGrowthStage → Sprite 오버라이드 (BN과 분리)
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

namespace IsoTilemap
{
    [CreateAssetMenu(fileName = "PlantOverlaySpriteCatalog", menuName = "Dist/Farming/Plant Overlay Sprite Catalog")]
    public sealed class PlantOverlaySpriteCatalog : ScriptableObject
    {
        public const string ResourcesLoadName = "PlantOverlaySpriteCatalog";
        public const string DefaultAssetPath = "Assets/Dist/Resources/PlantOverlaySpriteCatalog.asset";
        public const string AssetPath = DefaultAssetPath;

        [Serializable]
        public sealed class StageEntry
        {
            public PlantGrowthStage Stage;
            public Sprite Sprite;
        }

        [Serializable]
        public sealed class SeedStageEntry
        {
            public string SeedItemId;
            public PlantGrowthStage Stage;
            public Sprite Sprite;
        }

        [SerializeField] List<StageEntry> _stages = new();
        [SerializeField] List<SeedStageEntry> _seedStages = new();

        Dictionary<PlantGrowthStage, Sprite> _stageMap;
        Dictionary<string, Sprite> _seedStageMap;

        void OnEnable() => RebuildCache();

        public void RebuildCache()
        {
            _stageMap = new Dictionary<PlantGrowthStage, Sprite>();
            for (int i = 0; i < _stages.Count; i++)
            {
                StageEntry entry = _stages[i];
                if (entry == null || entry.Sprite == null)
                    continue;
                _stageMap[entry.Stage] = entry.Sprite;
            }

            _seedStageMap = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            for (int i = 0; i < _seedStages.Count; i++)
            {
                SeedStageEntry entry = _seedStages[i];
                if (entry == null ||
                    string.IsNullOrEmpty(entry.SeedItemId) ||
                    entry.Sprite == null)
                    continue;
                _seedStageMap[SeedStageKey(entry.SeedItemId, entry.Stage)] = entry.Sprite;
            }
        }

        /// <summary>Assigned override only (no BN fallback).</summary>
        public Sprite GetAssigned(PlantGrowthStage stage, string seedItemId = null)
        {
            if (_stageMap == null || _seedStageMap == null)
                RebuildCache();

            if (!string.IsNullOrEmpty(seedItemId) &&
                _seedStageMap.TryGetValue(SeedStageKey(seedItemId, stage), out Sprite bySeed) &&
                bySeed != null)
                return bySeed;

            return _stageMap.TryGetValue(stage, out Sprite byStage) ? byStage : null;
        }

        public void SetStageSprite(PlantGrowthStage stage, Sprite sprite)
        {
            if (_stageMap == null)
                RebuildCache();

            int existing = FindStageIndex(stage);
            if (sprite == null)
            {
                if (existing >= 0)
                    _stages.RemoveAt(existing);
                _stageMap.Remove(stage);
                return;
            }

            if (existing >= 0)
                _stages[existing].Sprite = sprite;
            else
                _stages.Add(new StageEntry { Stage = stage, Sprite = sprite });

            _stageMap[stage] = sprite;
        }

        static string SeedStageKey(string seedItemId, PlantGrowthStage stage) =>
            seedItemId + "\0" + ((int)stage).ToString();

        int FindStageIndex(PlantGrowthStage stage)
        {
            for (int i = 0; i < _stages.Count; i++)
            {
                StageEntry entry = _stages[i];
                if (entry != null && entry.Stage == stage)
                    return i;
            }

            return -1;
        }
    }
}
