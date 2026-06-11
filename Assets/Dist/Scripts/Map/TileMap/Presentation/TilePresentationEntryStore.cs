using System;
using System.Collections.Generic;

namespace IsoTilemap
{
    /// <summary>
    /// 타일 표현 entry 레지스트리. 합성하지 않으며 Set/Remove/Query만 제공합니다.
    /// 관여하지 않은 Source(entry 없음·비활성)는 Query에서 제외됩니다.
    /// </summary>
    public sealed class TilePresentationEntryStore
    {
        readonly struct EntryKey : IEquatable<EntryKey>
        {
            public readonly Guid TileId;
            public readonly PresentationConcern Concern;
            public readonly PresentationSource Source;

            public EntryKey(Guid tileId, PresentationConcern concern, PresentationSource source)
            {
                TileId = tileId;
                Concern = concern;
                Source = source;
            }

            public bool Equals(EntryKey other) =>
                TileId.Equals(other.TileId) && Concern == other.Concern && Source == other.Source;

            public override bool Equals(object obj) => obj is EntryKey other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(TileId, Concern, Source);
        }

        readonly Dictionary<EntryKey, TilePresentationEntry> _entries = new();
        readonly HashSet<PresentationSource> _sourceEngaged = new();
        readonly Dictionary<PresentationSource, HashSet<Guid>> _engagedTilesBySource = new();
        readonly List<TilePresentationEntry> _queryScratch = new();

        public void Set(PresentationConcern concern, PresentationSource source, Guid tileId, float scalar01)
        {
            int priority = PresentationPriorityTable.Get(source);
            var entry = new TilePresentationEntry(concern, source, tileId, scalar01, priority);
            var key = new EntryKey(tileId, concern, source);
            _entries[key] = entry;
            MarkSourceEngaged(source, tileId);
        }

        public bool Remove(PresentationConcern concern, PresentationSource source, Guid tileId)
        {
            var key = new EntryKey(tileId, concern, source);
            if (!_entries.Remove(key))
                return false;

            UnmarkTileEngaged(source, tileId);
            return true;
        }

        public void SetSourceEngaged(PresentationSource source, bool engaged)
        {
            if (engaged)
            {
                _sourceEngaged.Add(source);
                return;
            }

            ClearSource(source);
        }

        public bool IsSourceEngaged(PresentationSource source) => _sourceEngaged.Contains(source);

        public bool IsSourceEngagedForTile(Guid tileId, PresentationSource source) =>
            _engagedTilesBySource.TryGetValue(source, out HashSet<Guid> tiles) && tiles.Contains(tileId);

        public void ApplyOcclusionDelta(
            PresentationSource source,
            PresentationConcern concern,
            in TileOcclusionPresentationDelta delta)
        {
            if (delta.IsEmpty)
                return;

            IReadOnlyList<(Guid tileId, float occlusion01)> apply = delta.ApplyEntries;
            for (int i = 0; i < apply.Count; i++)
            {
                (Guid tileId, float occlusion01) e = apply[i];
                Set(concern, source, e.tileId, e.occlusion01);
            }

            IReadOnlyList<Guid> clear = delta.ClearIds;
            for (int i = 0; i < clear.Count; i++)
                Remove(concern, source, clear[i]);
        }

        public IReadOnlyList<TilePresentationEntry> Query(in PresentationQuery query)
        {
            _queryScratch.Clear();

            foreach (KeyValuePair<EntryKey, TilePresentationEntry> kv in _entries)
            {
                TilePresentationEntry entry = kv.Value;

                if (query.TileId.HasValue && entry.TileId != query.TileId.Value)
                    continue;

                if (query.Concern.HasValue && entry.Concern != query.Concern.Value)
                    continue;

                if (query.Source.HasValue && entry.Source != query.Source.Value)
                    continue;

                if (query.OnlyEngagedSources && !_sourceEngaged.Contains(entry.Source))
                    continue;

                if (query.OnlyEngagedForTile && query.TileId.HasValue &&
                    !IsSourceEngagedForTile(entry.TileId, entry.Source))
                    continue;

                _queryScratch.Add(entry);
            }

            return _queryScratch;
        }

        public bool TryGetEngagedEntry(
            Guid tileId,
            PresentationConcern concern,
            PresentationSource source,
            out TilePresentationEntry entry)
        {
            entry = default;
            if (!IsSourceEngagedForTile(tileId, source))
                return false;

            var key = new EntryKey(tileId, concern, source);
            return _entries.TryGetValue(key, out entry);
        }

        /// <summary>Source가 관여 중인 타일의 scalar 스냅샷 (delta 이전 값 비교용).</summary>
        public void CopyScalarsForSource(
            PresentationSource source,
            PresentationConcern concern,
            Dictionary<Guid, float> into)
        {
            into.Clear();
            if (!_engagedTilesBySource.TryGetValue(source, out HashSet<Guid> tiles))
                return;

            foreach (Guid tileId in tiles)
            {
                var key = new EntryKey(tileId, concern, source);
                if (_entries.TryGetValue(key, out TilePresentationEntry entry))
                    into[tileId] = entry.Scalar01;
            }
        }

        public void CollectEngagedTileIds(PresentationSource source, List<Guid> into)
        {
            into.Clear();
            if (!_engagedTilesBySource.TryGetValue(source, out HashSet<Guid> tiles))
                return;

            foreach (Guid tileId in tiles)
                into.Add(tileId);
        }

        public void GetEngagedSources(List<PresentationSource> into)
        {
            into.Clear();
            foreach (PresentationSource source in _sourceEngaged)
                into.Add(source);
        }

        public void GetEngagedSourcesForTile(Guid tileId, List<PresentationSource> into)
        {
            into.Clear();
            foreach (KeyValuePair<PresentationSource, HashSet<Guid>> kv in _engagedTilesBySource)
            {
                if (kv.Value.Contains(tileId))
                    into.Add(kv.Key);
            }
        }

        void ClearSource(PresentationSource source)
        {
            _sourceEngaged.Remove(source);
            _engagedTilesBySource.Remove(source);

            var toRemove = new List<EntryKey>();
            foreach (KeyValuePair<EntryKey, TilePresentationEntry> kv in _entries)
            {
                if (kv.Key.Source == source)
                    toRemove.Add(kv.Key);
            }

            for (int i = 0; i < toRemove.Count; i++)
                _entries.Remove(toRemove[i]);
        }

        void MarkSourceEngaged(PresentationSource source, Guid tileId)
        {
            _sourceEngaged.Add(source);
            if (!_engagedTilesBySource.TryGetValue(source, out HashSet<Guid> tiles))
            {
                tiles = new HashSet<Guid>();
                _engagedTilesBySource[source] = tiles;
            }

            tiles.Add(tileId);
        }

        void UnmarkTileEngaged(PresentationSource source, Guid tileId)
        {
            if (_engagedTilesBySource.TryGetValue(source, out HashSet<Guid> tiles))
            {
                tiles.Remove(tileId);
                if (tiles.Count == 0)
                    _engagedTilesBySource.Remove(source);
            }

            if (!HasAnyEntryForSource(source))
                _sourceEngaged.Remove(source);
        }

        bool HasAnyEntryForSource(PresentationSource source)
        {
            foreach (KeyValuePair<EntryKey, TilePresentationEntry> kv in _entries)
            {
                if (kv.Key.Source == source)
                    return true;
            }

            return false;
        }
    }
}
