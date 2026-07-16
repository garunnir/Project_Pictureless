// ============================================================
// ItemIconCatalog — itemId → Sprite 매핑 (JSON과 분리된 비주얼 SSOT)
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemIconCatalog", menuName = "Dist/Item Icon Catalog")]
public sealed class ItemIconCatalog : ScriptableObject
{
    public const string ResourcesLoadName = "ItemIconCatalog";
    public const string AssetPath = "Assets/Dist/Resources/ItemIconCatalog.asset";

    [Serializable]
    public sealed class Entry
    {
        public string ItemId;
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
            if (entry == null || string.IsNullOrEmpty(entry.ItemId) || entry.Icon == null)
                continue;

            _map[entry.ItemId] = entry.Icon;
        }
    }

    public Sprite Resolve(string itemId)
    {
        if (_map == null)
            RebuildCache();

        if (!string.IsNullOrEmpty(itemId) &&
            _map.TryGetValue(itemId, out Sprite icon) &&
            icon != null)
            return icon;

        return _defaultIcon;
    }

    public Sprite GetAssignedIcon(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return null;

        if (_map == null)
            RebuildCache();

        return _map.TryGetValue(itemId, out Sprite icon) ? icon : null;
    }

    public void SetDefaultIcon(Sprite sprite) => _defaultIcon = sprite;

    /// <summary>Editor: assign or clear per-item icon. Null sprite removes the entry.</summary>
    public void SetIcon(string itemId, Sprite sprite)
    {
        if (string.IsNullOrEmpty(itemId))
            return;

        if (_map == null)
            RebuildCache();

        int existing = FindEntryIndex(itemId);
        if (sprite == null)
        {
            if (existing >= 0)
                _entries.RemoveAt(existing);
            _map.Remove(itemId);
            return;
        }

        if (existing >= 0)
            _entries[existing].Icon = sprite;
        else
            _entries.Add(new Entry { ItemId = itemId, Icon = sprite });

        _map[itemId] = sprite;
    }

    int FindEntryIndex(string itemId)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            Entry entry = _entries[i];
            if (entry != null && entry.ItemId == itemId)
                return i;
        }

        return -1;
    }
}
