// ============================================================
// QualityVisualPresenter — 도구 품질(CUT, HAMMER) → 표시용 itemId·후보
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

public static class QualityVisualPresenter
{
    const string ToolItemType = "TOOL";

    sealed class QualityItemRef
    {
        public string itemId;
        public int level;
    }

    static readonly Dictionary<string, List<QualityItemRef>> ItemsByQuality =
        new Dictionary<string, List<QualityItemRef>>(StringComparer.Ordinal);

    static bool _built;

    /// <summary>
    /// 품질 칸 기본 아이콘 itemId. 해당 품질을 가진 대표 아이템.
    /// </summary>
    public static string GetIconItemId(string qualityId)
    {
        if (string.IsNullOrEmpty(qualityId))
            return string.Empty;

        EnsureCache();
        if (!ItemsByQuality.TryGetValue(qualityId, out List<QualityItemRef> list) ||
            list == null ||
            list.Count == 0)
            return string.Empty;

        return list[0].itemId;
    }

    public static void FillItemIds(string qualityId, int minLevel, List<string> dest)
    {
        if (dest == null)
            return;

        dest.Clear();
        if (string.IsNullOrEmpty(qualityId))
            return;

        EnsureCache();
        if (!ItemsByQuality.TryGetValue(qualityId, out List<QualityItemRef> list) || list == null)
            return;

        for (int i = 0; i < list.Count; i++)
        {
            QualityItemRef entry = list[i];
            if (entry == null || string.IsNullOrEmpty(entry.itemId) || entry.level < minLevel)
                continue;
            dest.Add(entry.itemId);
        }
    }

    public static int GetItemQualityLevel(string itemId, string qualityId)
    {
        if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(qualityId))
            return 0;

        EnsureCache();
        if (!ItemsByQuality.TryGetValue(qualityId, out List<QualityItemRef> list) || list == null)
            return ReadLevelFromItem(itemId, qualityId);

        for (int i = 0; i < list.Count; i++)
        {
            QualityItemRef entry = list[i];
            if (entry != null && entry.itemId == itemId)
                return entry.level;
        }

        return ReadLevelFromItem(itemId, qualityId);
    }

    public static void Invalidate()
    {
        ItemsByQuality.Clear();
        _built = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Invalidate();

    static int ReadLevelFromItem(string itemId, string qualityId)
    {
        ItemData item = GameplayData.GetItem(itemId);
        if (item?.qualities == null)
            return 0;

        int best = 0;
        for (int q = 0; q < item.qualities.Count; q++)
        {
            QualityEntry quality = item.qualities[q];
            if (quality == null || quality.id != qualityId)
                continue;
            if (quality.level > best)
                best = quality.level;
        }

        return best;
    }

    static void EnsureCache()
    {
        if (_built)
            return;

        _built = true;
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        CollectFrom(GameplayData.GameItems?.Items, seenIds);
        CollectFrom(GameplayData.RefData?.Items, seenIds);
        SortAll();
    }

    static void CollectFrom(IReadOnlyList<ItemData> items, HashSet<string> seenIds)
    {
        if (items == null)
            return;

        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = items[i];
            if (item == null || string.IsNullOrEmpty(item.id) || !seenIds.Add(item.id))
                continue;
            if (item.qualities == null)
                continue;

            for (int q = 0; q < item.qualities.Count; q++)
            {
                QualityEntry quality = item.qualities[q];
                if (quality == null || string.IsNullOrEmpty(quality.id))
                    continue;
                AddItem(quality.id, item.id, quality.level);
            }
        }
    }

    static void AddItem(string qualityId, string itemId, int level)
    {
        if (!ItemsByQuality.TryGetValue(qualityId, out List<QualityItemRef> list))
        {
            list = new List<QualityItemRef>(8);
            ItemsByQuality[qualityId] = list;
        }

        for (int i = 0; i < list.Count; i++)
        {
            QualityItemRef existing = list[i];
            if (existing == null || existing.itemId != itemId)
                continue;
            if (level > existing.level)
                existing.level = level;
            return;
        }

        list.Add(new QualityItemRef { itemId = itemId, level = level });
    }

    static void SortAll()
    {
        foreach (KeyValuePair<string, List<QualityItemRef>> pair in ItemsByQuality)
        {
            List<QualityItemRef> list = pair.Value;
            if (list == null || list.Count <= 1)
                continue;

            string qualityId = pair.Key;
            list.Sort((a, b) => Compare(a, b, qualityId));
        }
    }

    static int Compare(QualityItemRef a, QualityItemRef b, string qualityId)
    {
        string idA = a?.itemId;
        string idB = b?.itemId;
        if (string.IsNullOrEmpty(idA) && string.IsNullOrEmpty(idB))
            return 0;
        if (string.IsNullOrEmpty(idA))
            return 1;
        if (string.IsNullOrEmpty(idB))
            return -1;

        int scoreCmp = Score(GameplayData.GetItem(idB), qualityId)
            .CompareTo(Score(GameplayData.GetItem(idA), qualityId));
        if (scoreCmp != 0)
            return scoreCmp;
        return string.CompareOrdinal(idA, idB);
    }

    static int Score(ItemData item, string qualityId)
    {
        if (item == null || string.IsNullOrEmpty(item.id))
            return int.MinValue;

        int score = 0;
        if (string.Equals(item.id, qualityId, StringComparison.OrdinalIgnoreCase))
            score += 1000;
        if (item.type == ToolItemType)
            score += 200;
        if (HasIcon(item.id))
            score += 100;
        score -= item.id.Length;
        return score;
    }

    static bool HasIcon(string itemId)
    {
        ItemIconCatalog catalog = ItemVisualPresenter.Catalog;
        if (catalog != null && catalog.GetAssignedIcon(itemId) != null)
            return true;
        return BnTilesetIconResolver.Contains(itemId);
    }
}
