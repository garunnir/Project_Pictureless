// ============================================================
// ConstructionData — Dist 맵 건설 레시피 POCO (GameData constructions.json)
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    [Serializable]
    public sealed class ConstructionsFileRoot
    {
        public List<ConstructionData> constructions;
    }

    [Serializable]
    public sealed class ConstructionData
    {
        public string id;
        public string category;
        public string display_name;
        public string skill_used;
        public List<SkillReq> skills_required;
        public int difficulty;
        public float time_minutes;
        public List<QualityEntry> qualities_required;
        public List<ToolSlot> tools;
        public List<ComponentSlot> components;
        /// <summary>Dist TileDefinition.prefabId.</summary>
        public string post_prefab_id;
        /// <summary>HorizontalFace | OccupiedCell | VerticalFace</summary>
        public string post_slot;
        public List<string> pre_flags;
        public List<string> deny_flags;
    }
}
