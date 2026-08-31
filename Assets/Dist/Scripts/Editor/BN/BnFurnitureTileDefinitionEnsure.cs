// ============================================================
// BnFurnitureTileDefinitionEnsure — BN 가구 id용 TileDefinition 자리 + Crate 폴백
// ============================================================

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using IsoTilemap;
using UnityEditor;
using UnityEngine;

static class BnFurnitureTileDefinitionEnsure
{
    const string StubFolder = "Assets/Dist/SOData/Tile/Furniture/BN";
    const string CrateDefinitionPath = "Assets/Dist/SOData/Tile/Furniture/Crate.asset";
    const string PrefabDbPath = "Assets/Dist/SOData/Tile/Tile Prefab DB.asset";
    const string FurnitureIdsPath = "Assets/StreamingAssets/BNData/mapgen/furniture_ids.json";
    const string HousesFolder = "Assets/StreamingAssets/BNData/mapgen/houses";
    const string BnCategory = "BN";

    [Serializable]
    class FurnitureIdsFile
    {
        public string[] ids;
    }

    [MenuItem(DistMcpMenus.BnEnsureHouseFurnitureTiles)]
    static void EnsureFromMenu()
    {
        EnsureStubs();
    }

    public static void EnsureStubs()
    {
        TileDefinition crate = AssetDatabase.LoadAssetAtPath<TileDefinition>(CrateDefinitionPath);
        if (crate == null || crate.prefab == null)
        {
            Debug.LogError(
                $"[BnFurnitureTileDefinitionEnsure] Crate fallback missing: {CrateDefinitionPath}");
            return;
        }

        TilePrefabDB db = AssetDatabase.LoadAssetAtPath<TilePrefabDB>(PrefabDbPath);
        if (db == null)
        {
            Debug.LogError($"[BnFurnitureTileDefinitionEnsure] Prefab DB missing: {PrefabDbPath}");
            return;
        }

        List<string> ids = LoadStubIds();
        if (ids.Count == 0)
        {
            Debug.LogWarning(
                "[BnFurnitureTileDefinitionEnsure] No BN furniture ids. Run export_mapgen.py first.");
            return;
        }

        DistScriptableObjectEnsure.EnsureParentFoldersForAsset(StubFolder + "/_.asset");

        int created = 0;
        int updated = 0;
        int registered = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < ids.Count; i++)
        {
            string id = ids[i];
            if (string.IsNullOrEmpty(id) || !seen.Add(id))
                continue;

            string path = StubFolder + "/" + SafeAssetName(id) + ".asset";
            TileDefinition def = AssetDatabase.LoadAssetAtPath<TileDefinition>(path);
            bool isNew = def == null;
            if (isNew)
            {
                def = ScriptableObject.CreateInstance<TileDefinition>();
                def.prefabId = id;
                def.prefab = crate.prefab;
                def.thumbnail = crate.thumbnail;
                def.category = BnCategory;
                def.size = crate.size;
                def.placementSlot = TilePlacementSlot.OccupiedCell;
                def.occupied = crate.occupied;
                def.edge = crate.edge;
                AssetDatabase.CreateAsset(def, path);
                created++;
            }
            else
            {
                bool dirty = false;
                if (string.IsNullOrEmpty(def.prefabId))
                {
                    def.prefabId = id;
                    dirty = true;
                }

                if (def.prefab == null)
                {
                    def.prefab = crate.prefab;
                    dirty = true;
                }

                if (def.placementSlot == TilePlacementSlot.None)
                {
                    def.placementSlot = TilePlacementSlot.OccupiedCell;
                    dirty = true;
                }

                if (dirty)
                {
                    EditorUtility.SetDirty(def);
                    updated++;
                }
            }

            if (RegisterInDb(db, def))
                registered++;
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"[BnFurnitureTileDefinitionEnsure] ids={seen.Count} created={created} " +
            $"updated={updated} db+={registered}");
    }

    static List<string> LoadStubIds()
    {
        var ids = new List<string>();
        if (File.Exists(FurnitureIdsPath))
        {
            string text = File.ReadAllText(FurnitureIdsPath);
            FurnitureIdsFile parsed = JsonUtility.FromJson<FurnitureIdsFile>(text);
            if (parsed?.ids != null)
            {
                for (int i = 0; i < parsed.ids.Length; i++)
                {
                    if (!string.IsNullOrEmpty(parsed.ids[i]))
                        ids.Add(parsed.ids[i]);
                }
            }
        }

        if (ids.Count > 0)
            return ids;

        return ScanHousePrefabIds();
    }

    static List<string> ScanHousePrefabIds()
    {
        var ids = new List<string>();
        if (!AssetDatabase.IsValidFolder(HousesFolder))
            return ids;

        string[] guids = AssetDatabase.FindAssets("", new[] { HousesFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;
            string text = File.ReadAllText(path);
            CollectFPrefixedIds(text, ids);
        }

        return ids;
    }

    static void CollectFPrefixedIds(string json, List<string> ids)
    {
        const string key = "\"prefabId\": \"";
        int cursor = 0;
        while (cursor < json.Length)
        {
            int at = json.IndexOf(key, cursor, StringComparison.Ordinal);
            if (at < 0)
                break;
            int start = at + key.Length;
            int end = json.IndexOf('"', start);
            if (end < 0)
                break;
            string id = json.Substring(start, end - start);
            if (id.StartsWith("f_", StringComparison.Ordinal))
                ids.Add(id);
            cursor = end + 1;
        }
    }

    static bool RegisterInDb(TilePrefabDB db, TileDefinition def)
    {
        if (db.entries == null)
            db.entries = new List<TileDefinition>();

        for (int i = 0; i < db.entries.Count; i++)
        {
            TileDefinition existing = db.entries[i];
            if (existing == def)
                return false;
            if (existing != null &&
                string.Equals(existing.prefabId, def.prefabId, StringComparison.Ordinal))
                return false;
        }

        db.entries.Add(def);
        return true;
    }

    static string SafeAssetName(string id)
    {
        var chars = new char[id.Length];
        for (int i = 0; i < id.Length; i++)
        {
            char ch = id[i];
            chars[i] = char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '_';
        }

        return new string(chars);
    }
}
#endif
