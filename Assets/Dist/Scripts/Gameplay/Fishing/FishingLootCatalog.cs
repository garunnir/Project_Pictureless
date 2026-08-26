// ============================================================
// FishingLootCatalog — 낚시 루트 가중치·성공률 SSOT
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using UnityEngine;

[CreateAssetMenu(fileName = "FishingLootCatalog", menuName = "Dist/Fishing/Loot Catalog")]
public sealed class FishingLootCatalog : ScriptableObject
{
    public const string DefaultAssetPath =
        "Assets/Dist/SOData/Gameplay/Fishing/FishingLootCatalog.asset";

    public const string ResourcesAssetPath =
        "Assets/Dist/Resources/Fishing/FishingLootCatalog.asset";

    /// <summary>Player build Resources.Load SSOT (확장자 제외).</summary>
    public const string ResourcesLoadName = "Fishing/FishingLootCatalog";

    [Serializable]
    public sealed class LootEntry
    {
        public string ItemId = "fish";
        [Min(0f)] public float Weight = 1f;
    }

    [SerializeField, Range(0f, 1f)] float _baseCatchChance = 0.65f;
    [SerializeField] List<LootEntry> _entries = new()
    {
        new LootEntry { ItemId = "fish", Weight = 80f },
        new LootEntry { ItemId = "crayfish", Weight = 12f },
        new LootEntry { ItemId = "fish_smoked", Weight = 3f }
    };

    public float BaseCatchChance => Mathf.Clamp01(_baseCatchChance);

    public bool TryRollCatch(ItemData rod, out string itemId)
    {
        itemId = null;
        float catchChance = ResolveCatchChance(rod);
        if (UnityEngine.Random.value > catchChance)
            return false;

        if (!TryPickLoot(rod, out itemId))
            return false;

        return !string.IsNullOrEmpty(itemId);
    }

    public float ResolveCatchChance(ItemData rod)
    {
        float chance = BaseCatchChance;
        chance *= ResolveRodCatchMultiplier(rod);
        return Mathf.Clamp01(chance);
    }

    float ResolveRodCatchMultiplier(ItemData rod)
    {
        if (rod == null)
            return 1f;

        float multiplier = 1f;
        if (MapFishService.HasItemFlag(rod, MapFishConsts.FishPoorFlag))
            multiplier *= MapFishConsts.FishPoorCatchMultiplier;
        if (MapFishService.HasItemFlag(rod, MapFishConsts.FishGoodFlag))
            multiplier *= MapFishConsts.FishGoodCatchMultiplier;

        int fishingLevel = MapFishService.ResolveFishingQualityLevel(rod);
        if (fishingLevel > MapFishConsts.MinFishingQualityLevel)
            multiplier += (fishingLevel - MapFishConsts.MinFishingQualityLevel) *
                          MapFishConsts.FishingQualityLevelCatchBonus;

        return Mathf.Max(0f, multiplier);
    }

    bool TryPickLoot(ItemData rod, out string itemId)
    {
        itemId = null;
        IReadOnlyList<LootEntry> entries = _entries;
        if (entries == null || entries.Count == 0)
        {
            itemId = MapFishConsts.DefaultFishItemId;
            return true;
        }

        float lootMultiplier = ResolveLootWeightMultiplier(rod);
        float total = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            LootEntry entry = entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.ItemId) || entry.Weight <= 0f)
                continue;
            total += entry.Weight * lootMultiplier;
        }

        if (total <= 0f)
        {
            itemId = MapFishConsts.DefaultFishItemId;
            return true;
        }

        float roll = UnityEngine.Random.Range(0f, total);
        float cursor = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            LootEntry entry = entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.ItemId) || entry.Weight <= 0f)
                continue;

            cursor += entry.Weight * lootMultiplier;
            if (roll < cursor)
            {
                itemId = entry.ItemId;
                return true;
            }
        }

        itemId = entries[entries.Count - 1].ItemId;
        return !string.IsNullOrEmpty(itemId);
    }

    float ResolveLootWeightMultiplier(ItemData rod)
    {
        if (rod == null)
            return 1f;

        float multiplier = 1f;
        if (MapFishService.HasItemFlag(rod, MapFishConsts.FishPoorFlag))
            multiplier *= MapFishConsts.FishPoorLootMultiplier;
        if (MapFishService.HasItemFlag(rod, MapFishConsts.FishGoodFlag))
            multiplier *= MapFishConsts.FishGoodLootMultiplier;
        return Mathf.Max(0f, multiplier);
    }

    static FishingLootCatalog _runtime;

    public static FishingLootCatalog Runtime
    {
        get => _runtime;
        set => _runtime = value;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void DomainReset() => _runtime = null;

    public static FishingLootCatalog ResolveRuntimeCatalog() =>
        _runtime != null ? _runtime : _runtime = Resources.Load<FishingLootCatalog>(ResourcesLoadName);
}
