// ============================================================
// LocalizationTable — 키/문구 ScriptableObject 테이블 (언어별 에셋 1개)
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UI_ko", menuName = "Dist/Localization Table")]
public sealed class LocalizationTable : ScriptableObject, ILocalizationSource
{
    /// <summary>Resources.Load 경로 — ConstDataTable.AssetPath.LocalizeTable.UI SSOT.</summary>
    public static string ResourcesLoadName => ConstDataTable.AssetPath.LocalizeTable.UI;

    public const string AssetPath = "Assets/Dist/Resources/Localization/UI_ko.asset";

    [Serializable]
    public sealed class Entry
    {
        public string key;
        public string text;
    }

    [SerializeField] List<Entry> _entries = new List<Entry>();

    Dictionary<string, string> _map;

    public IReadOnlyList<Entry> Entries => _entries;

    void OnEnable() => RebuildCache();

    public void RebuildCache()
    {
        _map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (_entries == null)
            return;

        for (int i = 0; i < _entries.Count; i++)
        {
            Entry entry = _entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.key))
                continue;

            _map[entry.key] = entry.text ?? string.Empty;
        }
    }

    public bool TryGet(string key, out string text)
    {
        if (_map == null)
            RebuildCache();

        if (string.IsNullOrEmpty(key))
        {
            text = null;
            return false;
        }

        return _map.TryGetValue(key, out text);
    }

#if UNITY_EDITOR
    /// <summary>Editor: 키 목록을 덮어쓴다 (빈 키는 무시).</summary>
    public void EditorSetEntries(IList<Entry> entries)
    {
        _entries = new List<Entry>();
        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.key))
                    continue;
                _entries.Add(new Entry { key = entry.key, text = entry.text ?? string.Empty });
            }
        }

        RebuildCache();
    }
#endif
}
