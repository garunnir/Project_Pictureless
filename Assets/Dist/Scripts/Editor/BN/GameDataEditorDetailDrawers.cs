// ============================================================
// GameDataEditorDetailDrawers — 게임 디테일 필드 인스펙터 UI
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEditor;
using UnityEngine;

static class GameDataEditorDetailDrawers
{
    const float LabelWidth = 140f;

    public static void DrawItemDetailReadOnly(ItemData item)
    {
        if (item == null)
            return;

        DrawItemCommonDetailReadOnly(item);
        DrawNestedDetailReadOnly(item);
        DrawBookGatingReadOnly(item);
    }

    public static void DrawItemDetailEditable(ItemData item, Action markDirty)
    {
        if (item == null)
            return;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Game Detail", EditorStyles.boldLabel);

        EditInt(markDirty, "Max stack", ref item.max_stack);
        EditField(markDirty, "Subcategory", ref item.subcategory);
        EditTextArea(markDirty, "Description", ref item.description);
        EditBool(markDirty, "Has durability", ref item.has_durability);
        EditField(markDirty, "Repairs like", ref item.repairs_like);
        EditInt(markDirty, "Repair difficulty", ref item.repair_difficulty);
        EditInt(markDirty, "Bashing", ref item.bashing);
        EditInt(markDirty, "Cutting", ref item.cutting);
        EditInt(markDirty, "To hit", ref item.to_hit);

        EditStringList(markDirty, "Weapon category", ref item.weapon_category);
        EditStringList(markDirty, "Techniques", ref item.techniques);
        EditStringList(markDirty, "Materials", ref item.materials);
        EditStringList(markDirty, "Flags", ref item.flags);
        EditQualityList(markDirty, "Qualities", ref item.qualities);

        EditField(markDirty, "Comestible type", ref item.comestible_type);
        EditBool(markDirty, "Is container", ref item.is_container);
        EditField(markDirty, "Container id", ref item.container_id);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Book gating", EditorStyles.miniBoldLabel);
        EditField(markDirty, "Book skill", ref item.book_skill);
        EditInt(markDirty, "Book required level", ref item.book_required_level);
        EditInt(markDirty, "Book max level", ref item.book_max_level);

        DrawNestedDetailEditable(item, markDirty);
    }

    public static void DrawRecipeDetailReadOnly(RecipeData recipe)
    {
        if (recipe == null)
            return;

        if (recipe.autolearn_skills is { Count: > 0 })
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Autolearn skills", EditorStyles.miniBoldLabel);
            foreach (SkillReq skill in recipe.autolearn_skills)
                ReadField($"  {skill.skill}", $"lv{skill.level}");
        }

        if (recipe.proficiencies is { Count: > 0 })
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Proficiencies", EditorStyles.miniBoldLabel);
            foreach (ProficiencyReq prof in recipe.proficiencies)
            {
                string req = prof.required ? "required" : "optional";
                ReadField($"  {prof.proficiency}", $"{req}, x{prof.time_multiplier:0.##}");
            }
        }

        if (!string.IsNullOrEmpty(recipe.activity_level))
            ReadField("Activity level", recipe.activity_level);
        if (recipe.morale_modifier != 0)
            ReadField("Morale modifier", recipe.morale_modifier.ToString());
        if (recipe.hot_result)
            ReadField("Hot result", "yes");
        if (recipe.dehydrating)
            ReadField("Dehydrating", "yes");
    }

    public static void DrawRecipeDetailEditable(RecipeData recipe, Action markDirty)
    {
        if (recipe == null)
            return;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Recipe detail", EditorStyles.boldLabel);

        EditField(markDirty, "Activity level", ref recipe.activity_level);
        EditInt(markDirty, "Morale modifier", ref recipe.morale_modifier);
        EditBool(markDirty, "Hot result", ref recipe.hot_result);
        EditBool(markDirty, "Dehydrating", ref recipe.dehydrating);

        EditSkillReqList(markDirty, "Required skills", ref recipe.skills_required);
        EditSkillReqList(markDirty, "Autolearn skills", ref recipe.autolearn_skills);
        EditProficiencyList(markDirty, "Proficiencies", ref recipe.proficiencies);
        EditQualityList(markDirty, "Required qualities", ref recipe.qualities_required);
        DrawEditableTools(recipe, markDirty);
    }

    public static void DrawEditableComponentFlags(ComponentAlt alt, Action markDirty)
    {
        if (alt == null)
            return;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(54f);
        EditBoolInline(markDirty, "container", ref alt.container, 70f);
        EditBoolInline(markDirty, "filthy", ref alt.filthy, 52f);
        EditBoolInline(markDirty, "liquid", ref alt.liquid, 52f);
        EditorGUILayout.EndHorizontal();
    }

    static void DrawItemCommonDetailReadOnly(ItemData item)
    {
        if (!string.IsNullOrEmpty(item.subcategory))
            ReadField("Subcategory", item.subcategory);
        if (item.max_stack > 1)
            ReadField("Max stack", item.max_stack.ToString());

        if (!string.IsNullOrEmpty(item.description))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Description", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(item.description, EditorStyles.wordWrappedLabel);
        }

        ReadField("Has durability", item.has_durability ? "yes" : "no");

        if (!string.IsNullOrEmpty(item.repairs_like))
            ReadField("Repairs like", item.repairs_like);
        if (item.repair_difficulty != 0)
            ReadField("Repair difficulty", item.repair_difficulty.ToString());
        if (item.bashing != 0)
            ReadField("Bashing", item.bashing.ToString());
        if (item.cutting != 0)
            ReadField("Cutting", item.cutting.ToString());
        if (item.to_hit != 0)
            ReadField("To hit", item.to_hit.ToString());

        if (item.weapon_category is { Count: > 0 })
            ReadField("Weapon category", string.Join(", ", item.weapon_category));
        if (item.techniques is { Count: > 0 })
            ReadField("Techniques", string.Join(", ", item.techniques));
    }

    static void DrawBookGatingReadOnly(ItemData item)
    {
        if (string.IsNullOrEmpty(item.book_skill) && item.book_required_level == 0 && item.book_max_level == 0)
            return;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Book gating", EditorStyles.miniBoldLabel);
        if (!string.IsNullOrEmpty(item.book_skill))
            ReadField("Book skill", item.book_skill);
        if (item.book_required_level != 0)
            ReadField("Required level", item.book_required_level.ToString());
        if (item.book_max_level != 0)
            ReadField("Max level", item.book_max_level.ToString());
    }

    static void DrawNestedDetailReadOnly(ItemData item)
    {
        if (item.armor != null)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Armor", EditorStyles.miniBoldLabel);
            if (item.armor.covers is { Count: > 0 })
                ReadField("  Covers", string.Join(", ", item.armor.covers));
            ReadField("  Coverage", item.armor.coverage.ToString());
            ReadField("  Encumbrance", item.armor.encumbrance.ToString());
            ReadField("  Warmth", item.armor.warmth.ToString());
            ReadField("  Storage", item.armor.storage.ToString());
            ReadField("  Env. protection", item.armor.environmental_protection.ToString());
            ReadField("  Mat. thickness", item.armor.material_thickness.ToString());
        }

        if (item.gun != null)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Gun", EditorStyles.miniBoldLabel);
            if (!string.IsNullOrEmpty(item.gun.skill))
                ReadField("  Skill", item.gun.skill);
            if (item.gun.ammo is { Count: > 0 })
                ReadField("  Ammo", string.Join(", ", item.gun.ammo));
            ReadField("  Durability", item.gun.durability.ToString());
            ReadField("  Clip size", item.gun.clip_size.ToString());
            ReadField("  Range", item.gun.range.ToString());
            ReadField("  Recoil", item.gun.recoil.ToString());
        }

        if (item.tool != null)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Tool", EditorStyles.miniBoldLabel);
            ReadField("  Charges", $"{item.tool.initial_charges}/{item.tool.max_charges}");
            ReadField("  Per use", item.tool.charges_per_use.ToString());
            if (!string.IsNullOrEmpty(item.tool.revert_to))
                ReadField("  Revert to", item.tool.revert_to);
        }

        if (item.comestible != null)
            DrawComestibleReadOnly(item.comestible);
        if (item.ammo != null)
            DrawAmmoReadOnly(item.ammo);
        if (item.magazine != null)
            DrawMagazineReadOnly(item.magazine);
        if (item.book != null)
            DrawBookDetailReadOnly(item.book);
        if (item.container_detail != null)
            DrawContainerDetailReadOnly(item.container_detail);
    }

    static void DrawNestedDetailEditable(ItemData item, Action markDirty)
    {
        EditorGUILayout.Space(4);
        DrawArmorBlock(item, markDirty);
        DrawGunBlock(item, markDirty);
        DrawToolBlock(item, markDirty);
        DrawComestibleBlock(item, markDirty);
        DrawAmmoBlock(item, markDirty);
        DrawMagazineBlock(item, markDirty);
        DrawBookBlock(item, markDirty);
        DrawContainerDetailBlock(item, markDirty);
    }

    static void DrawArmorBlock(ItemData item, Action markDirty)
    {
        DrawNestedBlockHeader("Armor", item.armor != null, () => item.armor = new ArmorDetailData(), () => item.armor = null, markDirty);
        if (item.armor == null)
            return;

        var armor = item.armor;
        EditStringList(markDirty, "  Covers", ref armor.covers);
        EditInt(markDirty, "  Coverage", ref armor.coverage);
        EditInt(markDirty, "  Encumbrance", ref armor.encumbrance);
        EditInt(markDirty, "  Max encumbrance", ref armor.max_encumbrance);
        EditInt(markDirty, "  Warmth", ref armor.warmth);
        EditInt(markDirty, "  Storage", ref armor.storage);
        EditInt(markDirty, "  Env. protection", ref armor.environmental_protection);
        EditInt(markDirty, "  Mat. thickness", ref armor.material_thickness);
        EditBool(markDirty, "  Power armor", ref armor.power_armor);
    }

    static void DrawGunBlock(ItemData item, Action markDirty)
    {
        DrawNestedBlockHeader("Gun", item.gun != null, () => item.gun = new GunDetailData(), () => item.gun = null, markDirty);
        if (item.gun == null)
            return;

        var gun = item.gun;
        EditField(markDirty, "  Skill", ref gun.skill);
        EditStringList(markDirty, "  Ammo", ref gun.ammo);
        EditInt(markDirty, "  Ranged damage", ref gun.ranged_damage);
        EditInt(markDirty, "  Range", ref gun.range);
        EditInt(markDirty, "  Dispersion", ref gun.dispersion);
        EditInt(markDirty, "  Recoil", ref gun.recoil);
        EditInt(markDirty, "  Durability", ref gun.durability);
        EditInt(markDirty, "  Clip size", ref gun.clip_size);
        EditInt(markDirty, "  Reload", ref gun.reload);
        EditInt(markDirty, "  Burst", ref gun.burst);
    }

    static void DrawToolBlock(ItemData item, Action markDirty)
    {
        DrawNestedBlockHeader("Tool", item.tool != null, () => item.tool = new ToolDetailData(), () => item.tool = null, markDirty);
        if (item.tool == null)
            return;

        var tool = item.tool;
        EditInt(markDirty, "  Max charges", ref tool.max_charges);
        EditInt(markDirty, "  Initial charges", ref tool.initial_charges);
        EditInt(markDirty, "  Charges per use", ref tool.charges_per_use);
        EditInt(markDirty, "  Turns per charge", ref tool.turns_per_charge);
        EditStringList(markDirty, "  Ammo", ref tool.ammo);
        EditField(markDirty, "  Revert to", ref tool.revert_to);
    }

    static void DrawComestibleBlock(ItemData item, Action markDirty)
    {
        DrawNestedBlockHeader("Comestible", item.comestible != null, () => item.comestible = new ComestibleDetailData(), () => item.comestible = null, markDirty);
        if (item.comestible == null)
            return;

        var c = item.comestible;
        EditInt(markDirty, "  Calories", ref c.calories);
        EditInt(markDirty, "  Quench", ref c.quench);
        EditInt(markDirty, "  Fun", ref c.fun);
        EditFloat(markDirty, "  Spoils (min)", ref c.spoils_in_minutes);
        EditInt(markDirty, "  Charges", ref c.charges);
        EditInt(markDirty, "  Healthy", ref c.healthy);
        EditInt(markDirty, "  Stim", ref c.stim);
        EditField(markDirty, "  Addiction type", ref c.addiction_type);
    }

    static void DrawAmmoBlock(ItemData item, Action markDirty)
    {
        DrawNestedBlockHeader("Ammo", item.ammo != null, () => item.ammo = new AmmoDetailData(), () => item.ammo = null, markDirty);
        if (item.ammo == null)
            return;

        var ammo = item.ammo;
        EditField(markDirty, "  Ammo type", ref ammo.ammo_type);
        EditInt(markDirty, "  Damage", ref ammo.damage);
        EditInt(markDirty, "  Pierce", ref ammo.pierce);
        EditInt(markDirty, "  Range", ref ammo.range);
        EditInt(markDirty, "  Dispersion", ref ammo.dispersion);
        EditInt(markDirty, "  Recoil", ref ammo.recoil);
        EditInt(markDirty, "  Count", ref ammo.count);
    }

    static void DrawMagazineBlock(ItemData item, Action markDirty)
    {
        DrawNestedBlockHeader("Magazine", item.magazine != null, () => item.magazine = new MagazineDetailData(), () => item.magazine = null, markDirty);
        if (item.magazine == null)
            return;

        var mag = item.magazine;
        EditStringList(markDirty, "  Ammo type", ref mag.ammo_type);
        EditInt(markDirty, "  Capacity", ref mag.capacity);
        EditField(markDirty, "  Default ammo", ref mag.default_ammo);
        EditInt(markDirty, "  Reliability", ref mag.reliability);
        EditInt(markDirty, "  Reload time", ref mag.reload_time);
    }

    static void DrawBookBlock(ItemData item, Action markDirty)
    {
        DrawNestedBlockHeader("Book detail", item.book != null, () => item.book = new BookDetailData(), () => item.book = null, markDirty);
        if (item.book == null)
            return;

        var book = item.book;
        EditInt(markDirty, "  Intelligence", ref book.intelligence);
        EditInt(markDirty, "  Fun", ref book.fun);
        EditInt(markDirty, "  Chapters", ref book.chapters);
        EditFloat(markDirty, "  Read time (min)", ref book.read_time_minutes);
    }

    static void DrawContainerDetailBlock(ItemData item, Action markDirty)
    {
        DrawNestedBlockHeader("Container detail", item.container_detail != null, () => item.container_detail = new ContainerDetailData(), () => item.container_detail = null, markDirty);
        if (item.container_detail == null)
            return;

        var container = item.container_detail;
        EditBool(markDirty, "  Seals", ref container.seals);
        EditBool(markDirty, "  Watertight", ref container.watertight);
        EditBool(markDirty, "  Preserves", ref container.preserves);
    }

    static void DrawComestibleReadOnly(ComestibleDetailData c)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Comestible", EditorStyles.miniBoldLabel);
        if (c.calories != 0) ReadField("  Calories", c.calories.ToString());
        if (c.quench != 0) ReadField("  Quench", c.quench.ToString());
        if (c.fun != 0) ReadField("  Fun", c.fun.ToString());
        if (c.charges != 0) ReadField("  Charges", c.charges.ToString());
        if (!string.IsNullOrEmpty(c.addiction_type)) ReadField("  Addiction", c.addiction_type);
    }

    static void DrawAmmoReadOnly(AmmoDetailData ammo)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Ammo", EditorStyles.miniBoldLabel);
        if (!string.IsNullOrEmpty(ammo.ammo_type)) ReadField("  Type", ammo.ammo_type);
        ReadField("  Damage", ammo.damage.ToString());
        ReadField("  Count", ammo.count.ToString());
    }

    static void DrawMagazineReadOnly(MagazineDetailData mag)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Magazine", EditorStyles.miniBoldLabel);
        if (mag.ammo_type is { Count: > 0 }) ReadField("  Ammo type", string.Join(", ", mag.ammo_type));
        ReadField("  Capacity", mag.capacity.ToString());
    }

    static void DrawBookDetailReadOnly(BookDetailData book)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Book detail", EditorStyles.miniBoldLabel);
        ReadField("  Intelligence", book.intelligence.ToString());
        ReadField("  Fun", book.fun.ToString());
        ReadField("  Chapters", book.chapters.ToString());
        if (book.read_time_minutes > 0f)
            ReadField("  Read time", $"{book.read_time_minutes} min");
    }

    static void DrawContainerDetailReadOnly(ContainerDetailData container)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Container detail", EditorStyles.miniBoldLabel);
        ReadField("  Seals", container.seals ? "yes" : "no");
        ReadField("  Watertight", container.watertight ? "yes" : "no");
        ReadField("  Preserves", container.preserves ? "yes" : "no");
    }

    static void DrawEditableTools(RecipeData recipe, Action markDirty)
    {
        recipe.tools ??= new List<ToolSlot>();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Tools", EditorStyles.miniBoldLabel);

        int removeSlot = -1;
        for (int i = 0; i < recipe.tools.Count; i++)
        {
            ToolSlot slot = recipe.tools[i];
            slot.alternatives ??= new List<ToolAlt>();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Slot {i + 1}", GUILayout.Width(50));

            int removeAlt = -1;
            for (int j = 0; j < slot.alternatives.Count; j++)
            {
                if (j > 0)
                    EditorGUILayout.LabelField("OR", GUILayout.Width(20));

                ToolAlt alt = slot.alternatives[j];
                string newTool = EditorGUILayout.TextField(alt.tool, GUILayout.Width(100));
                int newCharges = EditorGUILayout.IntField(alt.charges, GUILayout.Width(40));
                if (newTool != alt.tool || newCharges != alt.charges)
                {
                    alt.tool = newTool;
                    alt.charges = newCharges;
                    markDirty();
                }

                if (GUILayout.Button("x", GUILayout.Width(20)))
                    removeAlt = j;
            }

            if (GUILayout.Button("+alt", GUILayout.Width(36)))
            {
                slot.alternatives.Add(new ToolAlt { tool = "tool_id", charges = -1 });
                markDirty();
            }

            if (GUILayout.Button("-", GUILayout.Width(20)))
                removeSlot = i;

            EditorGUILayout.EndHorizontal();

            if (removeAlt >= 0)
            {
                slot.alternatives.RemoveAt(removeAlt);
                markDirty();
            }
        }

        if (removeSlot >= 0)
        {
            recipe.tools.RemoveAt(removeSlot);
            markDirty();
        }

        if (GUILayout.Button("+ Add Tool Slot", GUILayout.Width(120)))
        {
            recipe.tools.Add(new ToolSlot
            {
                alternatives = new List<ToolAlt> { new() { tool = "tool_id", charges = -1 } }
            });
            markDirty();
        }
    }

    static void DrawNestedBlockHeader(string title, bool hasBlock, Action add, Action remove, Action markDirty)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel, GUILayout.Width(LabelWidth));
        if (!hasBlock)
        {
            if (GUILayout.Button("Add", GUILayout.Width(48)))
            {
                add();
                markDirty();
            }
        }
        else if (GUILayout.Button("Remove", GUILayout.Width(64)))
        {
            remove();
            markDirty();
        }

        EditorGUILayout.EndHorizontal();
    }

    static void EditSkillReqList(Action markDirty, string label, ref List<SkillReq> list)
    {
        list ??= new List<SkillReq>();
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

        int removeAt = -1;
        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            SkillReq req = list[i];
            string skill = EditorGUILayout.TextField(req.skill ?? "", GUILayout.MinWidth(80));
            int level = EditorGUILayout.IntField(req.level, GUILayout.Width(40));
            if (skill != req.skill || level != req.level)
            {
                req.skill = skill;
                req.level = level;
                markDirty();
            }

            if (GUILayout.Button("x", GUILayout.Width(20)))
                removeAt = i;
            EditorGUILayout.EndHorizontal();
        }

        if (removeAt >= 0)
        {
            list.RemoveAt(removeAt);
            markDirty();
        }

        if (GUILayout.Button($"+ {label}", GUILayout.Width(LabelWidth + 20)))
        {
            list.Add(new SkillReq { skill = "fabrication", level = 0 });
            markDirty();
        }
    }

    static void EditProficiencyList(Action markDirty, string label, ref List<ProficiencyReq> list)
    {
        list ??= new List<ProficiencyReq>();
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

        int removeAt = -1;
        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            ProficiencyReq prof = list[i];
            string id = EditorGUILayout.TextField(prof.proficiency ?? "", GUILayout.MinWidth(100));
            bool required = EditorGUILayout.ToggleLeft("req", prof.required, GUILayout.Width(36));
            float mult = EditorGUILayout.FloatField(prof.time_multiplier, GUILayout.Width(48));
            if (id != prof.proficiency || required != prof.required || !Mathf.Approximately(mult, prof.time_multiplier))
            {
                prof.proficiency = id;
                prof.required = required;
                prof.time_multiplier = mult;
                markDirty();
            }

            if (GUILayout.Button("x", GUILayout.Width(20)))
                removeAt = i;
            EditorGUILayout.EndHorizontal();
        }

        if (removeAt >= 0)
        {
            list.RemoveAt(removeAt);
            markDirty();
        }

        if (GUILayout.Button($"+ {label}", GUILayout.Width(LabelWidth + 20)))
        {
            list.Add(new ProficiencyReq { proficiency = "prof_id", required = false, time_multiplier = 1f });
            markDirty();
        }
    }

    static void EditQualityList(Action markDirty, string label, ref List<QualityEntry> list)
    {
        list ??= new List<QualityEntry>();
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

        int removeAt = -1;
        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            QualityEntry entry = list[i];
            string id = EditorGUILayout.TextField(entry.id ?? "", GUILayout.MinWidth(80));
            int level = EditorGUILayout.IntField(entry.level, GUILayout.Width(40));
            if (id != entry.id || level != entry.level)
            {
                entry.id = id;
                entry.level = level;
                markDirty();
            }

            if (GUILayout.Button("x", GUILayout.Width(20)))
                removeAt = i;
            EditorGUILayout.EndHorizontal();
        }

        if (removeAt >= 0)
        {
            list.RemoveAt(removeAt);
            markDirty();
        }

        if (GUILayout.Button($"+ {label}", GUILayout.Width(LabelWidth + 20)))
        {
            list.Add(new QualityEntry { id = "HAMMER", level = 1 });
            markDirty();
        }
    }

    static void EditStringList(Action markDirty, string label, ref List<string> list)
    {
        list ??= new List<string>();
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

        int removeAt = -1;
        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            string value = EditorGUILayout.TextField(list[i] ?? "");
            if (value != list[i])
            {
                list[i] = value;
                markDirty();
            }

            if (GUILayout.Button("x", GUILayout.Width(20)))
                removeAt = i;
            EditorGUILayout.EndHorizontal();
        }

        if (removeAt >= 0)
        {
            list.RemoveAt(removeAt);
            markDirty();
        }

        if (GUILayout.Button($"+ {label}", GUILayout.Width(LabelWidth + 20)))
        {
            list.Add("");
            markDirty();
        }
    }

    static void EditField(Action markDirty, string label, ref string value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(LabelWidth));
        string newVal = EditorGUILayout.TextField(value ?? "");
        if (newVal != (value ?? ""))
        {
            value = newVal;
            markDirty();
        }

        EditorGUILayout.EndHorizontal();
    }

    static void EditTextArea(Action markDirty, string label, ref string value)
    {
        EditorGUILayout.LabelField(label);
        string newVal = EditorGUILayout.TextArea(value ?? "", GUILayout.MinHeight(48f));
        if (newVal != (value ?? ""))
        {
            value = newVal;
            markDirty();
        }
    }

    static void EditInt(Action markDirty, string label, ref int value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(LabelWidth));
        int newVal = EditorGUILayout.IntField(value);
        if (newVal != value)
        {
            value = newVal;
            markDirty();
        }

        EditorGUILayout.EndHorizontal();
    }

    static void EditFloat(Action markDirty, string label, ref float value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(LabelWidth));
        float newVal = EditorGUILayout.FloatField(value);
        if (!Mathf.Approximately(newVal, value))
        {
            value = newVal;
            markDirty();
        }

        EditorGUILayout.EndHorizontal();
    }

    static void EditBool(Action markDirty, string label, ref bool value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(LabelWidth));
        bool newVal = EditorGUILayout.Toggle(value);
        if (newVal != value)
        {
            value = newVal;
            markDirty();
        }

        EditorGUILayout.EndHorizontal();
    }

    static void EditBoolInline(Action markDirty, string label, ref bool value, float width)
    {
        bool newVal = EditorGUILayout.ToggleLeft(label, value, GUILayout.Width(width));
        if (newVal != value)
        {
            value = newVal;
            markDirty();
        }
    }

    static void ReadField(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(LabelWidth));
        EditorGUILayout.LabelField(value ?? "—", EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndHorizontal();
    }
}
