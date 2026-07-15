// ============================================================
// GameDataTypes — JSON 데이터 역직렬화용 POCO (아이템·레시피·컨테이너)
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    // ── Root wrappers (JsonUtility 최상위) ──────────────────────

    [Serializable]
    public sealed class ItemsFileRoot
    {
        public string _license;
        public string _source;
        public List<MaterialData> materials;
        public List<QualityData> qualities;
        public List<ItemData> items;
        public List<ContainerData> containers;
    }

    [Serializable]
    public sealed class RecipesFileRoot
    {
        public string _license;
        public string _source;
        public List<RecipeData> recipes;
        public List<RecipeData> uncraft;
    }

    // ── Item ───────────────────────────────────────────────────

    [Serializable]
    public sealed class ItemData
    {
        public string id;
        public string name;
        public string type;
        public string category;
        public int weight_g;
        public int volume_ml;
        public int max_stack = 1;
        public bool is_container;
        public string container_id;
        public List<string> materials;
        public List<string> flags;
        public List<QualityEntry> qualities;
        public string comestible_type;

        public float Weight => weight_g / 1000f;
        public float Volume => volume_ml / 1000f;
        public int MaxStack => max_stack > 0 ? max_stack : 1;
    }

    [Serializable]
    public sealed class MaterialData
    {
        public string id;
        public string name;
    }

    [Serializable]
    public sealed class QualityData
    {
        public string id;
        public string name;
    }

    [Serializable]
    public sealed class QualityEntry
    {
        public string id;
        public int level;
    }

    [Serializable]
    public sealed class ContainerData
    {
        public string id;
        public string name;
        public float max_weight;
        public float max_volume;

        public float MaxWeight => max_weight;
        public float MaxVolume => max_volume;
    }

    // ── Recipe ─────────────────────────────────────────────────

    [Serializable]
    public sealed class RecipeData
    {
        public string id;
        public string result;
        public string category;
        public string subcategory;
        public string skill_used;
        public List<SkillReq> skills_required;
        public int difficulty;
        public float time_minutes;
        public bool reversible;
        public bool autolearn;
        public int result_count;
        public List<QualityEntry> qualities_required;
        public List<ToolSlot> tools;
        public List<ComponentSlot> components;
        public bool is_uncraft;
        public List<BookLearn> book_learn;
        public List<Byproduct> byproducts;
    }

    [Serializable]
    public sealed class SkillReq
    {
        public string skill;
        public int level;
    }

    [Serializable]
    public sealed class ComponentSlot
    {
        public List<ComponentAlt> alternatives;
    }

    [Serializable]
    public sealed class ComponentAlt
    {
        public string item;
        public int count;
        public bool list;
    }

    [Serializable]
    public sealed class ToolSlot
    {
        public List<ToolAlt> alternatives;
    }

    [Serializable]
    public sealed class ToolAlt
    {
        public string tool;
        public int charges;
        public bool list;
    }

    [Serializable]
    public sealed class BookLearn
    {
        public string book;
        public int level;
    }

    [Serializable]
    public sealed class Byproduct
    {
        public string item;
        public int count;
    }
}
