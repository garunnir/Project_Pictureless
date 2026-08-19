// ============================================================
// ItemNameTable — catalog locale (name / description / recipe category)
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Garunnir.Runtime.Gameplay.Data
{
    public enum ItemLocaleKind
    {
        Name = 0,
        Description = 1,
        RecipeCategory = 2,
    }

    public static class ItemNameTable
    {
        public const string FileName = "item_names.json";
        public const string SectionNames = "names";
        public const string SectionDescriptions = "descriptions";
        public const string SectionRecipeCategories = "recipe_categories";
        const string MissingFormat = "[Missing: {0}.{1}]";
        const string MissingLogFormat = "[ItemNameTable] Missing catalog locale: {0}.{1}";

        static Dictionary<ItemLocaleKind, Dictionary<string, ItemNameEntry>> _merged;
        static Dictionary<ItemLocaleKind, Dictionary<string, ItemNameEntry>> _gameOverlay;
        static readonly HashSet<string> ReportedMissing = new HashSet<string>(StringComparer.Ordinal);
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
            public Dictionary<string, ItemNameEntry> descriptions;
            public Dictionary<string, ItemNameEntry> recipe_categories;
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
            _merged = CreateKindMaps();
            _gameOverlay = CreateKindMaps();
            ReportedMissing.Clear();
            _gameDirty = false;

            MergeFile(Path.Combine(Application.streamingAssetsPath, "BNData", FileName), overlay: false);
            MergeFile(Path.Combine(Application.streamingAssetsPath, "GameData", FileName), overlay: true);
        }

        public static void Unload()
        {
            _merged = null;
            _gameOverlay = null;
            ReportedMissing.Clear();
            _gameDirty = false;
        }

        static Dictionary<ItemLocaleKind, Dictionary<string, ItemNameEntry>> CreateKindMaps()
        {
            return new Dictionary<ItemLocaleKind, Dictionary<string, ItemNameEntry>>
            {
                [ItemLocaleKind.Name] = new Dictionary<string, ItemNameEntry>(StringComparer.Ordinal),
                [ItemLocaleKind.Description] = new Dictionary<string, ItemNameEntry>(StringComparer.Ordinal),
                [ItemLocaleKind.RecipeCategory] = new Dictionary<string, ItemNameEntry>(StringComparer.Ordinal),
            };
        }

        static Dictionary<string, ItemNameEntry> MapFor(
            Dictionary<ItemLocaleKind, Dictionary<string, ItemNameEntry>> maps,
            ItemLocaleKind kind)
        {
            return maps[kind];
        }

        static void MergeFile(string path, bool overlay)
        {
            if (!File.Exists(path))
                return;

            string json = File.ReadAllText(path);
            ItemNamesFileRoot root = GameDataJson.Deserialize<ItemNamesFileRoot>(json);
            if (root == null)
                return;

            MergeKind(root.names, ItemLocaleKind.Name, overlay);
            MergeKind(root.descriptions, ItemLocaleKind.Description, overlay);
            MergeKind(root.recipe_categories, ItemLocaleKind.RecipeCategory, overlay);
        }

        static void MergeKind(
            Dictionary<string, ItemNameEntry> source,
            ItemLocaleKind kind,
            bool overlay)
        {
            if (source == null)
                return;

            Dictionary<string, ItemNameEntry> merged = MapFor(_merged, kind);
            Dictionary<string, ItemNameEntry> overlayMap = MapFor(_gameOverlay, kind);
            foreach (KeyValuePair<string, ItemNameEntry> kv in source)
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Value == null)
                    continue;

                ItemNameEntry copy = CloneEntry(kv.Value);
                merged[kv.Key] = copy;
                if (overlay)
                    overlayMap[kv.Key] = CloneEntry(copy);
            }
        }

        static ItemNameEntry CloneEntry(ItemNameEntry src) => new()
        {
            en = src.en,
            ko = src.ko,
            ja = src.ja,
        };

        public static string Get(string itemId, DisplayLanguage language) =>
            Get(ItemLocaleKind.Name, itemId, language);

        public static string Get(ItemLocaleKind kind, string id, DisplayLanguage language)
        {
            EnsureLoaded();

            if (string.IsNullOrEmpty(id))
                return string.Empty;

            Dictionary<string, ItemNameEntry> merged = MapFor(_merged, kind);
            if (merged.TryGetValue(id, out ItemNameEntry entry) && entry != null)
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

            return ReportMissing(kind, id);
        }

        public static bool TryGetRaw(string itemId, DisplayLanguage language, out string text) =>
            TryGetRaw(ItemLocaleKind.Name, itemId, language, out text);

        public static bool TryGetRaw(
            ItemLocaleKind kind,
            string id,
            DisplayLanguage language,
            out string text)
        {
            EnsureLoaded();
            text = null;
            if (string.IsNullOrEmpty(id))
                return false;

            Dictionary<string, ItemNameEntry> merged = MapFor(_merged, kind);
            if (!merged.TryGetValue(id, out ItemNameEntry entry) || entry == null)
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

        public static void Set(string itemId, DisplayLanguage language, string text) =>
            Set(ItemLocaleKind.Name, itemId, language, text);

        public static void Set(ItemLocaleKind kind, string id, DisplayLanguage language, string text)
        {
            if (string.IsNullOrEmpty(id))
                return;

            EnsureLoaded();

            Dictionary<string, ItemNameEntry> mergedMap = MapFor(_merged, kind);
            if (!mergedMap.TryGetValue(id, out ItemNameEntry merged) || merged == null)
            {
                merged = new ItemNameEntry();
                mergedMap[id] = merged;
            }

            SetSlot(merged, language, text ?? string.Empty);

            Dictionary<string, ItemNameEntry> overlayMap = MapFor(_gameOverlay, kind);
            if (!overlayMap.TryGetValue(id, out ItemNameEntry overlay) || overlay == null)
            {
                overlay = new ItemNameEntry();
                overlayMap[id] = overlay;
            }

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
        public static void SeedFromItemNameIfMissing(string itemId, string legacyName, DisplayLanguage language) =>
            SeedIfMissing(ItemLocaleKind.Name, itemId, legacyName, language);

        public static void SeedFromDescriptionIfMissing(
            string itemId,
            string legacyDescription,
            DisplayLanguage language) =>
            SeedIfMissing(ItemLocaleKind.Description, itemId, legacyDescription, language);

        static void SeedIfMissing(
            ItemLocaleKind kind,
            string id,
            string legacyText,
            DisplayLanguage language)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(legacyText))
                return;

            EnsureLoaded();
            if (TryGetRaw(kind, id, language, out _))
                return;

            Set(kind, id, language, legacyText);
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
                names = CopyIfAny(MapFor(_gameOverlay, ItemLocaleKind.Name)),
                descriptions = CopyIfAny(MapFor(_gameOverlay, ItemLocaleKind.Description)),
                recipe_categories = CopyIfAny(MapFor(_gameOverlay, ItemLocaleKind.RecipeCategory)),
            };

            File.WriteAllText(path, GameDataJson.Serialize(root));
            _gameDirty = false;
            return true;
        }

        static Dictionary<string, ItemNameEntry> CopyIfAny(Dictionary<string, ItemNameEntry> source)
        {
            if (source == null || source.Count == 0)
                return null;

            return new Dictionary<string, ItemNameEntry>(source, StringComparer.Ordinal);
        }

        static string SectionName(ItemLocaleKind kind) => kind switch
        {
            ItemLocaleKind.Description => SectionDescriptions,
            ItemLocaleKind.RecipeCategory => SectionRecipeCategories,
            _ => SectionNames,
        };

        static string ReportMissing(ItemLocaleKind kind, string id)
        {
            string section = SectionName(kind);
            string reportKey = section + "." + id;
            if (ReportedMissing.Add(reportKey))
                Debug.LogError(string.Format(MissingLogFormat, section, id));

            return string.Format(MissingFormat, section, id);
        }

#if UNITY_EDITOR
        public static string GetGameOverlayPath() =>
            Path.Combine(Application.streamingAssetsPath, "GameData", FileName);

        public static string GetRefNamesPath() =>
            Path.Combine(Application.streamingAssetsPath, "BNData", FileName);
#endif
    }
}
