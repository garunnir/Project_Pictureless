// ============================================================
// MapHearingPingOverlay — 프레임 청각 핑 목록 (셀당 max audibility)
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public readonly struct HearingPingEntry
    {
        public readonly Vector3Int Cell;
        public readonly Vector3 WorldPos;
        public readonly float Alpha;

        public HearingPingEntry(Vector3Int cell, Vector3 worldPos, float alpha)
        {
            Cell = cell;
            WorldPos = worldPos;
            Alpha = alpha;
        }
    }

    public sealed class MapHearingPingOverlay
    {
        readonly List<HearingPingEntry> _entries = new(16);

        public IReadOnlyList<HearingPingEntry> Entries => _entries;
        public int Count => _entries.Count;

        public void Clear() => _entries.Clear();

        public void AddOrMax(Vector3Int cell, Vector3 worldPos, float alpha01)
        {
            float alpha = Mathf.Clamp01(alpha01);
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Cell != cell)
                    continue;

                if (alpha > _entries[i].Alpha)
                    _entries[i] = new HearingPingEntry(cell, worldPos, alpha);
                return;
            }

            _entries.Add(new HearingPingEntry(cell, worldPos, alpha));
        }
    }
}
