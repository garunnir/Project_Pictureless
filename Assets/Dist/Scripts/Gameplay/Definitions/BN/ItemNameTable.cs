// ============================================================
// ItemNameTable — item id → en/ko/ja 표시명 (BNData + GameData overlay)
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class ItemNameTable
    {
        public const string FileName = "item_names.json";
        const string MissingFormat = "[Missing: ItemName {0}]";

        static Dictionary<string, ItemNameEntry> _merged;
        static Dictionary<string, ItemNameEntry> _gameOverlay;
        static bool _gameDirty;

        [Serializable]
        public sealed class ItemNameEntry
        {
            public string en;
            public string ko;
            public string ja;
        }

        [Serializable]
        sealed class ItemNamesFileRoot
        {
            public string _license;
            public string _source;
            public Dictionary<string, ItemNameEntry> names;
        }

        public static bool IsGameDirty => _gameDirty;

        public static void EnsureLoaded()
        {
            if (_merged != null)
                return;
            Reload();
        }

        public static void Reload()
        {
            _merged = new Dictionary<string, ItemNameEntry>(StringComparer.Ordinal);
            _gameOverlay = new Dictionary<string, ItemNameEntry>(StringComparer.Ordinal);
            _gameDirty = false;

            MergeFile(Path.Combine(Application.streamingAssetsPath, "BNData", FileName), overlay: false);
            MergeFile(Path.Combine(Application.streamingAssetsPath, "GameData", FileName), overlay: true);
        }

        public static void Unload()
        {
            _merged = null;
            _gameOverlay = null;
            _gameDirty = false;
        }

        static void MergeFile(string path, bool overlay)
        {
            if (!File.Exists(path))
                return;

            string json = File.ReadAllText(path);
            ItemNamesFileRoot root = GameDataJson.Deserialize<ItemNamesFileRoot>(json);
            if (root?.names == null)
                return;

            foreach (KeyValuePair<string, ItemNameEntry> kv in root.names)
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Value == null)
                    continue;

                ItemNameEntry copy = CloneEntry(kv.Value);
                _merged[kv.Key] = copy;
                if (overlay)
                    _gameOverlay[kv.Key] = CloneEntry(copy);
            }
        }

        static ItemNameEntry CloneEntry(ItemNameEntry src) => new()
        {
            en = src.en,
            ko = src.ko,
            ja = src.ja,
        };

        public static string Get(string itemId, DisplayLanguage language)
        {
            EnsureLoaded();

            if (string.IsNullOrEmpty(itemId))
                return string.Empty;

            if (_merged.TryGetValue(itemId, out ItemNameEntry entry) && entry != null)
            {
                string primary = GetSlot(entry, language);
                if (!string.IsNullOrEmpty(primary))
                    return primary;

                if (language != DisplayLanguage.En)
                {
                    string en = entry.en;
                    if (!string.IsNullOrEmpty(en))
                        return en;
                }
            }

            return string.Format(MissingFormat, itemId);
        }

        public static bool TryGetRaw(string itemId, DisplayLanguage language, out string text)
        {
            EnsureLoaded();
            text = null;
            if (string.IsNullOrEmpty(itemId) ||
                !_merged.TryGetValue(itemId, out ItemNameEntry entry) ||
                entry == null)
                return false;

            text = GetSlot(entry, language);
            return !string.IsNullOrEmpty(text);
        }

        static string GetSlot(ItemNameEntry entry, DisplayLanguage language) => language switch
        {
            DisplayLanguage.En => entry.en,
            DisplayLanguage.Ja => entry.ja,
            _ => entry.ko,
        };

        public static void Set(string itemId, DisplayLanguage language, string text)
        {
            if (string.IsNullOrEmpty(itemId))
                return;

            EnsureLoaded();

            if (!_merged.TryGetValue(itemId, out ItemNameEntry merged) || merged == null)
            {
                merged = new ItemNameEntry();
                _merged[itemId] = merged;
            }

            SetSlot(merged, language, text ?? string.Empty);

            if (!_gameOverlay.TryGetValue(itemId, out ItemNameEntry overlay) || overlay == null)
            {
                overlay = new ItemNameEntry();
                _gameOverlay[itemId] = overlay;
            }

            // Overlay keeps full row snapshot for save (merge en from BN if needed)
            if (string.IsNullOrEmpty(overlay.en) && !string.IsNullOrEmpty(merged.en))
                overlay.en = merged.en;
            if (string.IsNullOrEmpty(overlay.ko) && !string.IsNullOrEmpty(merged.ko) &&
                language != DisplayLanguage.Ko)
                overlay.ko = merged.ko;
            if (string.IsNullOrEmpty(overlay.ja) && !string.IsNullOrEmpty(merged.ja) &&
                language != DisplayLanguage.Ja)
                overlay.ja = merged.ja;

            SetSlot(overlay, language, text ?? string.Empty);
            _gameDirty = true;
        }

        static void SetSlot(ItemNameEntry entry, DisplayLanguage language, string text)
        {
            switch (language)
            {
                case DisplayLanguage.En:
                    entry.en = text;
                    break;
                case DisplayLanguage.Ja:
                    entry.ja = text;
                    break;
                default:
                    entry.ko = text;
                    break;
            }
        }

        /// <summary>Custom items.json에만 있던 name을 GameData overlay ko로 시드.</summary>
        public static void SeedFromItemNameIfMissing(string itemId, string legacyName, DisplayLanguage language)
        {
            if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(legacyName))
                return;

            EnsureLoaded();
            if (TryGetRaw(itemId, language, out _))
                return;

            Set(itemId, language, legacyName);
        }

        public static bool SaveGameOverlay()
        {
            EnsureLoaded();
            string dir = Path.Combine(Application.streamingAssetsPath, "GameData");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, FileName);

            var root = new ItemNamesFileRoot
            {
                _license = "Project proprietary (overrides) + BN-derived where applicable",
                _source = "Data Definitions / GameData",
                names = new Dictionary<string, ItemNameEntry>(_gameOverlay, StringComparer.Ordinal),
            };

            File.WriteAllText(path, GameDataJson.Serialize(root));
            _gameDirty = false;
            return true;
        }

#if UNITY_EDITOR
        public static string GetGameOverlayPath() =>
            Path.Combine(Application.streamingAssetsPath, "GameData", FileName);

        public static string GetRefNamesPath() =>
            Path.Combine(Application.streamingAssetsPath, "BNData", FileName);
#endif
    }
}
