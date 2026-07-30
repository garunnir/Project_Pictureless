using System.Collections.Generic;
using UnityEngine;

namespace SpriteBaker
{
    /// <summary>
    /// Lookup + lifecycle for finished atlases. <see cref="SpriteAtlasBaker"/>
    /// stores results here when each bake completes; <see cref="AnimatedSpriteRenderer"/>
    /// (and Dist adapters) read from here at runtime.
    ///
    /// Runtime bakes <b>own</b> their Texture/Material (destroyed on Evict).
    /// <see cref="Register"/> entries do <b>not</b> own project assets.
    /// </summary>
    public static class SpriteAtlasCache
    {
        private struct Entry
        {
            public BakedSpriteAtlas Data;
            public bool OwnsResources;
        }

        private static readonly Dictionary<int, Entry> s_cache = new();

        public static bool TryGet(int key, out BakedSpriteAtlas data)
        {
            if (s_cache.TryGetValue(key, out var entry))
            {
                data = entry.Data;
                return true;
            }
            data = default;
            return false;
        }

        public static bool IsReady(int key) => s_cache.ContainsKey(key);

        /// <summary>Pre-baked atlas (editor Output). Does not take ownership.</summary>
        public static void Register(int key, BakedSpriteAtlas data)
        {
            if (s_cache.TryGetValue(key, out var existing) && existing.OwnsResources)
                DestroyOwned(existing.Data);

            s_cache[key] = new Entry { Data = data, OwnsResources = false };
        }

        internal static void StoreResult(int key, BakedSpriteAtlas data)
        {
            if (s_cache.TryGetValue(key, out var existing) && existing.OwnsResources)
                DestroyOwned(existing.Data);

            s_cache[key] = new Entry { Data = data, OwnsResources = true };
        }

        public static void Clear()
        {
            foreach (var entry in s_cache.Values)
            {
                if (entry.OwnsResources)
                    DestroyOwned(entry.Data);
            }
            s_cache.Clear();
        }

        public static void Evict(int key)
        {
            if (!s_cache.TryGetValue(key, out var entry))
                return;

            if (entry.OwnsResources)
                DestroyOwned(entry.Data);
            s_cache.Remove(key);
        }

        private static void DestroyOwned(BakedSpriteAtlas data)
        {
            if (data.Atlas != null) Object.Destroy(data.Atlas);
            if (data.SharedMaterial != null) Object.Destroy(data.SharedMaterial);
        }
    }
}
