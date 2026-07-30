// ============================================================
// SpriteBakerCatalog — animId → 사전베이크 시트 등록/조회
// ============================================================

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SpriteBaker;
using UnityEngine;

/// <summary>
/// Runtime catalog of pre-baked SpriteBaker sheets. Call
/// <see cref="RegisterAll"/> once before binding views.
/// </summary>
[CreateAssetMenu(fileName = "SpriteBakerCatalog", menuName = "Dist/SpriteBaker/Catalog")]
public sealed class SpriteBakerCatalog : ScriptableObject
{
    public const string IdleAnimId = "Idle";

    [Serializable]
    public sealed class Entry
    {
        [HorizontalGroup("row", Width = 140)]
        public string AnimId;

        [HorizontalGroup("row")]
        public SpriteBakerSheetAsset Sheet;
    }

    [SerializeField] string _characterId;
    [SerializeField] List<Entry> _entries = new();

    Dictionary<string, SpriteBakerSheetAsset> _map;

    public string CharacterId => _characterId;
    public IReadOnlyList<Entry> Entries => _entries;

    void OnEnable() => RebuildMap();

    public void RebuildMap()
    {
        _map = new Dictionary<string, SpriteBakerSheetAsset>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < _entries.Count; i++)
        {
            Entry e = _entries[i];
            if (e == null || e.Sheet == null || string.IsNullOrEmpty(e.AnimId))
                continue;
            _map[e.AnimId] = e.Sheet;
        }
    }

    /// <summary>Register every sheet into <see cref="SpriteAtlasCache"/> (no ownership).</summary>
    [Button("Register All (runtime cache)")]
    public void RegisterAll()
    {
        if (_map == null)
            RebuildMap();

        foreach (KeyValuePair<string, SpriteBakerSheetAsset> kv in _map)
        {
            SpriteBakerSheetAsset sheet = kv.Value;
            if (sheet == null || sheet.Atlas == null || sheet.SharedMaterial == null)
            {
                Debug.LogError(
                    $"[SpriteBakerCatalog] Sheet missing atlas/material for '{kv.Key}' on {name}.",
                    this);
                continue;
            }

            SpriteAtlasCache.Register(sheet.CacheKey, sheet.ToBakedAtlas());
        }
    }

    public bool TryGetSheet(string animId, out SpriteBakerSheetAsset sheet)
    {
        if (_map == null)
            RebuildMap();

        if (string.IsNullOrEmpty(animId))
        {
            sheet = null;
            return false;
        }

        return _map.TryGetValue(animId, out sheet) && sheet != null;
    }

    public bool TryGetCacheKey(string animId, out int key)
    {
        if (TryGetSheet(animId, out SpriteBakerSheetAsset sheet))
        {
            key = sheet.CacheKey;
            return true;
        }

        key = 0;
        return false;
    }

#if UNITY_EDITOR
    public void EditorUpsert(string animId, SpriteBakerSheetAsset sheet)
    {
        if (string.IsNullOrEmpty(animId) || sheet == null)
            return;

        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i] != null &&
                string.Equals(_entries[i].AnimId, animId, StringComparison.OrdinalIgnoreCase))
            {
                _entries[i].Sheet = sheet;
                RebuildMap();
                return;
            }
        }

        _entries.Add(new Entry { AnimId = animId, Sheet = sheet });
        RebuildMap();
    }

    public void EditorSetCharacterId(string id) => _characterId = id;
#endif
}
