#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace IsoTilemap.EditorTools
{
    public static class TileDefinitionPlacementSlotMigrationEditor
    {
        [MenuItem("Tools/Map/Migrate TileDefinitions placement slots")]
        static void MigratePlacementSlots()
        {
            var defs = Resources.FindObjectsOfTypeAll<TileDefinition>();
            int applied = 0;

            for (int i = 0; i < defs.Length; i++)
            {
                var def = defs[i];
                if (def == null || string.IsNullOrEmpty(def.prefabId))
                    continue;

                var slot = TileIdentityUtil.InferSlotFromPrefabId(def.prefabId);
                if (slot == TilePlacementSlot.None)
                    slot = TilePlacementSlot.OccupiedCell;

                if (def.placementSlot == slot)
                    continue;

                def.placementSlot = slot;
                EditorUtility.SetDirty(def);
                applied++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[TileDefinitionPlacementSlot] 완료: {applied}개 갱신");
        }
    }
}
#endif
