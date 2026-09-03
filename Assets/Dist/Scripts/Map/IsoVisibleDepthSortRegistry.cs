// ============================================================
// IsoVisibleDepthSortRegistry — 가시 투명 렌더러 sortOrder 0..N-1 재부여
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// Play 중 drawable 투명 <see cref="Renderer"/>만 모아 이소 키로 정렬한 뒤 연속 <c>sortingOrder</c>를 쓴다.
    /// </summary>
    public static class IsoVisibleDepthSortRegistry
    {
        struct Entry
        {
            public Renderer Renderer;
            public IsoDepthSortKey Key;
            public object Owner;
        }

        static readonly List<Entry> Entries = new();
        static readonly List<Entry> SortScratch = new();
        static bool Dirty;

        public static void Register(Renderer renderer, IsoDepthSortKey key, object owner)
        {
            if (renderer == null || owner == null)
                return;

            Entries.Add(new Entry
            {
                Renderer = renderer,
                Key = key,
                Owner = owner,
            });
            Dirty = true;
        }

        public static void UnregisterOwner(object owner)
        {
            if (owner == null)
                return;

            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                if (Entries[i].Owner == owner)
                    Entries.RemoveAt(i);
            }

            Dirty = true;
        }

        public static void Clear()
        {
            Entries.Clear();
            SortScratch.Clear();
            Dirty = false;
        }

        public static void MarkDirty() => Dirty = true;

        public static void RebuildIfDirty()
        {
            if (!Dirty)
                return;

            Dirty = false;
            SortScratch.Clear();

            for (int i = 0; i < Entries.Count; i++)
            {
                Entry entry = Entries[i];
                Renderer renderer = entry.Renderer;
                if (renderer == null)
                    continue;

                if (!renderer.enabled)
                    continue;

                if (!renderer.gameObject.activeInHierarchy)
                    continue;

                SortScratch.Add(entry);
            }

            SortScratch.Sort(CompareEntries);

            for (int i = 0; i < SortScratch.Count; i++)
                SortScratch[i].Renderer.sortingOrder = i;
        }

        static int CompareEntries(Entry a, Entry b)
        {
            int c = a.Key.CompareTo(b.Key);
            if (c != 0)
                return c;

            return a.Renderer.GetInstanceID().CompareTo(b.Renderer.GetInstanceID());
        }
    }
}
