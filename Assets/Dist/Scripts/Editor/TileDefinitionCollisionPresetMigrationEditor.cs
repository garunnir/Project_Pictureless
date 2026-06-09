#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace IsoTilemap.EditorTools
{
    public static class TileDefinitionCollisionPresetMigrationEditor
    {
        [MenuItem("Tools/Map/Apply collision presets to TileDefinitions")]
        static void ApplyCollisionPresets()
        {
            bool skipManual = EditorUtility.DisplayDialog(
                "TileDefinition 충돌 프리셋",
                "이미 충돌 설정이 있는 에셋을 건너뛸까요?",
                "수동 설정 스킵",
                "전체 덮어쓰기");

            var dbs = Resources.FindObjectsOfTypeAll<TilePrefabDB>();
            int applied = 0;
            int skipped = 0;

            for (int d = 0; d < dbs.Length; d++)
            {
                var db = dbs[d];
                if (db == null || db.entries == null)
                    continue;

                for (int i = 0; i < db.entries.Count; i++)
                {
                    var def = db.entries[i];
                    if (def == null || string.IsNullOrEmpty(def.prefabId))
                        continue;

                    if (skipManual && HasManualCollisionSettings(def))
                    {
                        skipped++;
                        continue;
                    }

                    ApplyPresetForPrefabId(def);
                    EditorUtility.SetDirty(def);
                    applied++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[TileDefinitionCollisionPreset] 완료: 적용 {applied}개, 스킵 {skipped}개");
        }

        static bool HasManualCollisionSettings(TileDefinition def)
        {
            var o = def.occupied;
            var e = def.edge;
            return o.providesLogicalFloor || o.blocksPassageAndOcclusion || o.usePhysicsCollider ||
                   o.splitPassageAndOcclusion || o.blocksOccupiedCells || o.occludesOccupiedCells ||
                   e.blocksPassageAndOcclusion || e.splitPassageAndOcclusion || e.blocksEdge ||
                   e.separatesRoom || e.occludesEdge;
        }

        static void ApplyPresetForPrefabId(TileDefinition def)
        {
            string id = def.prefabId;
            def.occupied = default;
            def.edge = default;

            if (id.StartsWith("Floor/", StringComparison.Ordinal))
            {
                def.occupied.providesLogicalFloor = true;
                return;
            }

            if (id.StartsWith("ThickWall/", StringComparison.Ordinal) ||
                id.StartsWith("Wall/", StringComparison.Ordinal))
            {
                def.occupied.blocksPassageAndOcclusion = true;
                return;
            }

            if (id.StartsWith("SlimWall/", StringComparison.Ordinal))
            {
                def.edge.blocksPassageAndOcclusion = true;
                def.edge.splitPassageAndOcclusion = false;
                return;
            }

            if (id.StartsWith("Slope/", StringComparison.Ordinal))
            {
                def.occupied.usePhysicsCollider = true;
                return;
            }

            if (id.StartsWith("Furniture/", StringComparison.Ordinal) ||
                id.StartsWith("Furniture/Box", StringComparison.Ordinal))
            {
                def.occupied.usePhysicsCollider = true;
            }
        }
    }
}
#endif
