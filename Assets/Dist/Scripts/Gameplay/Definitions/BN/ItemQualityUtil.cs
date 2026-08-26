// ============================================================
// ItemQualityUtil — BN item quality gate helpers (shared by farming, fishing, crafting)
// ============================================================

using System;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class ItemQualityUtil
    {
        public static bool HasQuality(ItemData item, string qualityId, int minLevel)
        {
            if (item?.qualities == null || string.IsNullOrEmpty(qualityId))
                return false;

            for (int i = 0; i < item.qualities.Count; i++)
            {
                QualityEntry quality = item.qualities[i];
                if (quality == null || string.IsNullOrEmpty(quality.id))
                    continue;
                if (!quality.id.Equals(qualityId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (quality.level >= minLevel)
                    return true;
            }

            return false;
        }
    }
}
