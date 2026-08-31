// ============================================================
// TraitIconCatalog — traitId → Sprite 매핑 (TraitIds와 분리된 비주얼 SSOT)
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TraitIconCatalog", menuName = "Dist/Character/Trait Icon Catalog")]
public sealed class TraitIconCatalog : ScriptableObject
{
    public const string DefaultAssetPath =
        "Assets/Dist/SOData/Gameplay/Character/TraitIconCatalog.asset";
    public const string ResourcesLoadName = "TraitIconCatalog";
    public const string ResourcesAssetPath = "Assets/Dist/Resources/TraitIconCatalog.asset";

    [Serializable]
    public sealed class Entry
    {
        public string TraitId;
        public Sprite Icon;
    }

    [SerializeField] Sprite _defaultIcon;
    [SerializeField] List<Entry> _entries = new();

    Dictionary<string, Sprite> _map;

    public Sprite DefaultIcon => _defaultIcon;
    public IReadOnlyList<Entry> Entries => _entries;

    void OnEnable() => RebuildCache();

    public void RebuildCache()
    {
        _map = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        for (int i = 0; i < _entries.Count; i++)
        {
            Entry entry = _entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.TraitId) || entry.Icon == null)
                continue;

            _map[entry.TraitId] = entry.Icon;
        }
    }

    public Sprite Resolve(string traitId)
    {
        if (_map == null)
            RebuildCache();

        if (!string.IsNullOrEmpty(traitId) &&
            _map.TryGetValue(traitId, out Sprite icon) &&
            icon != null)
            return icon;

        return _defaultIcon;
    }

    public Sprite GetAssignedIcon(string traitId)
    {
        if (string.IsNullOrEmpty(traitId))
            return null;

        if (_map == null)
            RebuildCache();

        return _map.TryGetValue(traitId, out Sprite icon) ? icon : null;
    }

    public void SetDefaultIcon(Sprite sprite) => _defaultIcon = sprite;

    /// <summary>Editor: assign or clear per-trait icon. Null sprite removes the entry.</summary>
    public void SetIcon(string traitId, Sprite sprite)
    {
        if (string.IsNullOrEmpty(traitId))
            return;

        if (_map == null)
            RebuildCache();

        int existing = FindEntryIndex(traitId);
        if (sprite == null)
        {
            if (existing >= 0)
                _entries.RemoveAt(existing);
            _map.Remove(traitId);
            return;
        }

        if (existing >= 0)
            _entries[existing].Icon = sprite;
        else
            _entries.Add(new Entry { TraitId = traitId, Icon = sprite });

        _map[traitId] = sprite;
    }

    int FindEntryIndex(string traitId)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            Entry entry = _entries[i];
            if (entry != null && entry.TraitId == traitId)
                return i;
        }

        return -1;
    }
}
