// ============================================================
// CraftingService — 합성 가능 여부 확인 + 재료 소비 + 결과 생성
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;
using Random = UnityEngine.Random;

public static class CraftingPseudoIds
{
    public const string Fire = "fire";
    public const string Apparatus = "apparatus";
    public const string Sunlight = "sunlight";
    public const string PseudoFlag = "PSEUDO";
    public const string CraftFlagFire = "FIRE";
    public const string CraftFlagLit = "LIT";
    public const string CraftFlagCook = "COOK";
    public const string CraftFlagSmoke = "SMOKE";
    public const string CraftFlagSmoker = "SMOKER";
    public const string RecipeFlagDark = "DARK";
    public const string MultiCookerTool = "multi_cooker";
    public const string CharSmokerTool = "char_smoker";
    public const string HotplateTool = "hotplate";
    public const string ToolsetTool = "toolset";
    public const string UseActionMulticooker = "multicooker";
    public static readonly string[] HeatToolIds =
    {
        HotplateTool,
        MultiCookerTool,
        CharSmokerTool,
        ToolsetTool,
    };
    public const int DefaultCookLikeMinutes = 10;
    public const int HotCoolMinutes = 60;
    public const int MinLightForDarkCraft = 1;
}

public interface ICraftingEnvironment
{
    bool HasPseudoTool(string toolId);
    bool HasEnvTool(string toolId);
    int GetEnvQualityLevel(string qualityId);
    int GetLightLevel();
    bool IsDaylight { get; }
}

public static class CraftingEnvironment
{
    public static ICraftingEnvironment Active { get; set; }
}

public static class CraftingWorldTime
{
    public static int AbsoluteWorldMinute { get; set; }
}

public static class CraftingLightGate
{
    public static bool RequiresLight(RecipeData recipe)
    {
        if (recipe?.flags == null || recipe.flags.Count == 0)
            return false;

        for (int i = 0; i < recipe.flags.Count; i++)
        {
            string flag = recipe.flags[i];
            if (string.IsNullOrEmpty(flag))
                continue;
            if (flag.Equals(CraftingPseudoIds.RecipeFlagDark, StringComparison.OrdinalIgnoreCase) ||
                flag.Equals("BLIND_EASY", StringComparison.OrdinalIgnoreCase) ||
                flag.Equals("BLIND_HARD", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static int ResolveLightLevel(CraftingMaterialPool pool)
    {
        int light = 0;
        ICraftingEnvironment env = CraftingEnvironment.Active;
        if (env != null)
            light = Math.Max(light, env.GetLightLevel());

        if (pool == null)
            return light;

        IReadOnlyList<InventoryContainer> sources = pool.Sources;
        for (int c = 0; c < sources.Count; c++)
        {
            IReadOnlyList<ItemStack> stacks = sources[c].Stacks;
            for (int s = 0; s < stacks.Count; s++)
            {
                ItemData item = stacks[s]?.Item;
                if (item?.flags == null)
                    continue;
                for (int f = 0; f < item.flags.Count; f++)
                {
                    string flag = item.flags[f];
                    if (string.IsNullOrEmpty(flag) ||
                        !flag.StartsWith("LIGHT_", StringComparison.OrdinalIgnoreCase))
                        continue;
                    string num = flag.Length > 6 ? flag.Substring(6) : string.Empty;
                    if (int.TryParse(num, out int level) && level > light)
                        light = level;
                }
            }
        }

        return light;
    }

    public static bool MeetsLight(RecipeData recipe, CraftingMaterialPool pool)
    {
        if (!RequiresLight(recipe))
            return true;
        return ResolveLightLevel(pool) >= CraftingPseudoIds.MinLightForDarkCraft;
    }
}

public static class CraftingService
{
    // BN disassembly 손실 판정 상수
    const float DamageRecoveryBase = 0.8f; // 0.8^damage_level
    const int MaxDamageLevel = 4;

    const int SkillDiceBase = 2;
    const int SkillDicePerLevel = 4; // 2 + 3*level + level
    const int SkillDiceSidesBase = 16;
    const int DifficultyDiceSides = 24;

    // Practice gain (현재 시스템 단순화)
    const int PracticeDifficultyMultiplier = 2;
    const int PracticeDifficultyBonus = 10;

    public static bool CanCraft(RecipeData recipe, InventoryContainer container)
    {
        if (container == null)
            return false;

        return CanCraft(recipe, new CraftingMaterialPool(new[] { container }));
    }

    public static bool CanCraft(RecipeData recipe, CraftingMaterialPool pool)
    {
        return CanCraft(recipe, pool, null, null);
    }

    public static bool CanCraft(
        RecipeData recipe,
        CraftingMaterialPool pool,
        IReadOnlyList<int> componentAltIndices,
        IReadOnlyList<int> toolAltIndices)
    {
        if (recipe == null || pool == null || string.IsNullOrEmpty(recipe.result))
            return false;

        if (!MeetsSkillRequirements(recipe))
            return false;

        if (!MeetsProficiencies(recipe))
            return false;

        if (!CraftingLightGate.MeetsLight(recipe, pool))
            return false;

        if (recipe.dehydrating &&
            !IsToolSatisfiedByEnvironment(CraftingPseudoIds.Sunlight) &&
            pool.CountItem(CraftingPseudoIds.Sunlight) <= 0)
            return false;

        if (!MeetsQualities(recipe, pool))
            return false;

        if (!MeetsToolSlots(recipe, pool, toolAltIndices))
            return false;

        return MeetsComponentSlots(recipe, pool, componentAltIndices);
    }

    public static float GetCraftTimeMinutes(RecipeData recipe, int quantity)
    {
        if (recipe == null || quantity <= 0)
            return 0f;
        float baseTime = recipe.time_minutes * quantity;
        float mult = ResolveProficiencyTimeMultiplier(recipe);
        return baseTime * mult;
    }

    /// <summary>cooks_like / smoking_result single-item transform.</summary>
    public static bool TryTransformComestible(
        ItemStack stack,
        InventoryContainer container,
        InventorySession session,
        string resultItemId,
        bool requireFire,
        bool requireApparatus)
    {
        if (stack?.Item == null || container == null || string.IsNullOrEmpty(resultItemId))
            return false;
        if (GameplayData.GetItem(resultItemId) == null)
            return false;

        if (requireFire &&
            !IsToolSatisfiedByEnvironment(CraftingPseudoIds.Fire) &&
            !HasChargedHeatTool(new CraftingMaterialPool(new[] { container })))
            return false;

        if (requireApparatus && !IsToolSatisfiedByEnvironment(CraftingPseudoIds.Apparatus))
            return false;

        if (container.TryTakeFromStack(stack, 1) <= 0)
            return false;

        container.AddItem(
            GameplayData.GetItem(resultItemId),
            1,
            0,
            cooked: true,
            hot: requireFire);

        session?.NotifyExternalStacksChanged(container);
        return true;
    }

    public static bool RecipeUsesTool(RecipeData recipe, string toolId)
    {
        if (recipe?.tools == null || string.IsNullOrEmpty(toolId))
            return false;
        for (int i = 0; i < recipe.tools.Count; i++)
        {
            ToolSlot slot = recipe.tools[i];
            if (slot?.alternatives == null)
                continue;
            for (int j = 0; j < slot.alternatives.Count; j++)
            {
                ToolAlt alt = slot.alternatives[j];
                if (alt != null &&
                    string.Equals(alt.tool, toolId, System.StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    public static int GetMaxCraftCount(
        RecipeData recipe,
        CraftingMaterialPool pool,
        IReadOnlyList<int> componentAltIndices,
        IReadOnlyList<int> toolAltIndices,
        int cap)
    {
        if (cap <= 0 || !CanCraft(recipe, pool, componentAltIndices, toolAltIndices))
            return 0;

        int max = cap;
        if (recipe.components != null)
        {
            for (int i = 0; i < recipe.components.Count; i++)
            {
                if (!TryPickComponentAlt(
                        recipe.components[i],
                        pool,
                        componentAltIndices,
                        i,
                        out ComponentAlt alt))
                    return 0;

                if (alt.count <= 0)
                    continue;

                int crafts = pool.CountItem(alt.item) / alt.count;
                if (crafts < max)
                    max = crafts;
            }
        }

        if (recipe.tools != null)
        {
            for (int i = 0; i < recipe.tools.Count; i++)
            {
                if (!TryPickToolAlt(
                        recipe.tools[i],
                        pool,
                        toolAltIndices,
                        i,
                        out ToolAlt alt))
                    return 0;

                if (alt.charges <= 0)
                    continue;

                int crafts = pool.CountToolCharges(alt.tool) / alt.charges;
                if (crafts < max)
                    max = crafts;
            }
        }

        return max < 0 ? 0 : max;
    }

    public static bool TryCraft(
        RecipeData recipe,
        InventoryContainer container,
        InventorySession session)
    {
        if (container == null)
            return false;

        return TryCraft(recipe, new CraftingMaterialPool(new[] { container }), session, null, null);
    }

    public static bool TryCraft(
        RecipeData recipe,
        CraftingMaterialPool pool,
        InventorySession session,
        IReadOnlyList<int> componentAltIndices,
        IReadOnlyList<int> toolAltIndices)
    {
        if (!TryCraftCore(recipe, pool, componentAltIndices, toolAltIndices))
            return false;

        NotifyPoolSourcesChanged(session, pool);
        return true;
    }

    public static int TryCraftMany(
        RecipeData recipe,
        CraftingMaterialPool pool,
        InventorySession session,
        IReadOnlyList<int> componentAltIndices,
        IReadOnlyList<int> toolAltIndices,
        int count)
    {
        if (count <= 0)
            return 0;

        int done = 0;
        for (int i = 0; i < count; i++)
        {
            if (!TryCraftCore(recipe, pool, componentAltIndices, toolAltIndices))
                break;
            done++;
        }

        if (done > 0)
            NotifyPoolSourcesChanged(session, pool);

        return done;
    }

    static bool TryCraftCore(
        RecipeData recipe,
        CraftingMaterialPool pool,
        IReadOnlyList<int> componentAltIndices,
        IReadOnlyList<int> toolAltIndices)
    {
        if (!CanCraft(recipe, pool, componentAltIndices, toolAltIndices))
            return false;

        if (recipe.components != null)
        {
            for (int i = 0; i < recipe.components.Count; i++)
            {
                if (!TryPickComponentAlt(
                        recipe.components[i],
                        pool,
                        componentAltIndices,
                        i,
                        out ComponentAlt alt))
                    return false;

                if (alt.count > 0 && !pool.TryRemoveItem(alt.item, alt.count))
                    return false;
            }
        }

        if (recipe.tools != null)
        {
            for (int i = 0; i < recipe.tools.Count; i++)
            {
                if (!TryPickToolAlt(
                        recipe.tools[i],
                        pool,
                        toolAltIndices,
                        i,
                        out ToolAlt alt))
                    return false;

                if (alt.charges > 0 && !IsEnvOnlyTool(alt.tool) &&
                    !pool.TryConsumeToolCharges(alt.tool, alt.charges))
                    return false;
            }
        }

        bool cooked = ShouldStampCooked(recipe);
        bool hot = recipe.hot_result;
        int resultCount = recipe.result_count > 0 ? recipe.result_count : 1;
        pool.TryAddResult(recipe.result, resultCount, cooked, hot);

        if (recipe.byproducts != null && recipe.byproducts.Count > 0)
        {
            for (int i = 0; i < recipe.byproducts.Count; i++)
            {
                Byproduct bp = recipe.byproducts[i];
                if (bp == null || string.IsNullOrEmpty(bp.item) || bp.count <= 0)
                    continue;
                pool.TryAddResult(bp.item, bp.count, cooked, hot: false);
            }
        }

        if (!string.IsNullOrEmpty(recipe.skill_used))
        {
            int practiceXp = recipe.difficulty * PracticeDifficultyMultiplier + PracticeDifficultyBonus;
            GameplayData.Stats.AddPractice(recipe.skill_used, practiceXp);
        }

        ApplyProficiencyPractice(recipe);
        ApplyCraftSideEffects(recipe);

        return true;
    }

    public static bool CanUncraft(RecipeData recipe, InventoryContainer container)
    {
        if (recipe == null || container == null || string.IsNullOrEmpty(recipe.result))
            return false;

        // tools 슬롯 게이팅(충전/소모 미구현 → 존재 여부만 검사)
        if (recipe.tools != null && recipe.tools.Count > 0)
        {
            foreach (ToolSlot slot in recipe.tools)
            {
                if (slot == null || slot.alternatives == null || slot.alternatives.Count == 0)
                    return false;

                bool slotSatisfied = false;
                for (int j = 0; j < slot.alternatives.Count; j++)
                {
                    ToolAlt alt = slot.alternatives[j];
                    if (alt == null || string.IsNullOrEmpty(alt.tool))
                        continue;

                    if (container.CountItem(alt.tool) > 0)
                    {
                        slotSatisfied = true;
                        break;
                    }
                }

                if (!slotSatisfied)
                    return false;
            }
        }

        // components는 "회수 결과"이므로 재료로서 존재 여부를 검사하지 않는다.
        return container.CountItem(recipe.result) > 0;
    }

    /// <summary>
    /// BN disassembly 손실 판정을 반영해서 1개만 제거하고 components를 회수한다.
    /// </summary>
    public static bool TryUncraft(
        RecipeData recipe,
        ItemStack disassembledStack,
        InventoryContainer container,
        InventorySession session)
    {
        if (recipe == null || disassembledStack == null || container == null)
            return false;

        if (disassembledStack.Item == null || disassembledStack.ItemId != recipe.result)
            return false;

        if (!CanUncraft(recipe, container))
            return false;

        int damageLevel = Mathf.Clamp(disassembledStack.DamageLevel, 0, MaxDamageLevel);

        // remove exactly 1 unit from the clicked stack (keep damage-level identity)
        if (disassembledStack.Count > 1)
            disassembledStack.SetCount(disassembledStack.Count - 1);
        else
            container.MutableStacks.Remove(disassembledStack);

        // BN: component_success_chance = min(0.8^damage, 1)
        float componentSuccessChance = Mathf.Pow(DamageRecoveryBase, damageLevel);
        if (componentSuccessChance > 1f) componentSuccessChance = 1f;

        // BN: skill dice vs difficulty dice
        int skillLevel = string.IsNullOrEmpty(recipe.skill_used)
            ? 0
            : GameplayData.Stats.GetSkillLevel(recipe.skill_used);
        int intCur = GameplayData.Stats.GetStat(AttributeIds.Int);

        int skillDice = SkillDiceBase + skillLevel * SkillDicePerLevel; // 2 + 3*level + level
        int skillSides = SkillDiceSidesBase + intCur;

        int diffDice = recipe.difficulty;
        int diffSides = DifficultyDiceSides;

        if (recipe.components != null)
        {
            for (int i = 0; i < recipe.components.Count; i++)
            {
                ComponentSlot slot = recipe.components[i];
                if (slot?.alternatives == null || slot.alternatives.Count == 0)
                    continue;

                ComponentAlt alt = slot.alternatives[0];
                if (alt == null || string.IsNullOrEmpty(alt.item) || alt.count <= 0)
                    continue;

                // 손실 판정 1: 스킬 대결 주사위(난이도!=0일 때만)
                bool compSuccess = true;
                if (recipe.difficulty != 0)
                    compSuccess = Dice(skillDice, skillSides) > Dice(diffDice, diffSides);

                if (recipe.difficulty != 0 && !compSuccess)
                    continue;

                // 손실 판정 2: 손상도 회수 확률
                bool dmgSuccess = componentSuccessChance > Random.value;
                if (!dmgSuccess)
                    continue;

                container.AddItem(alt.item, alt.count);
            }
        }

        // Practice gain (BN: difficulty*2, only when dis.skill_used exists)
        if (!string.IsNullOrEmpty(recipe.skill_used))
        {
            int practiceXp = recipe.difficulty * 2;
            if (practiceXp > 0)
                GameplayData.Stats.AddPractice(recipe.skill_used, practiceXp);
        }

        RecipeKnowledge.TryLearnFromDisassembly(recipe, GameplayData.RecipeMemory);

        session?.NotifyExternalStacksChanged(container);
        return true;
    }

    static bool MeetsSkillRequirements(RecipeData recipe)
    {
        if (!string.IsNullOrEmpty(recipe.skill_used))
        {
            int lv = GameplayData.Stats.GetSkillLevel(recipe.skill_used);
            if (lv < recipe.difficulty)
                return false;
        }

        if (recipe.skills_required == null || recipe.skills_required.Count == 0)
            return true;

        for (int i = 0; i < recipe.skills_required.Count; i++)
        {
            SkillReq req = recipe.skills_required[i];
            if (req == null || string.IsNullOrEmpty(req.skill))
                continue;

            if (GameplayData.Stats.GetSkillLevel(req.skill) < req.level)
                return false;
        }

        return true;
    }

    static bool MeetsQualities(RecipeData recipe, CraftingMaterialPool pool)
    {
        if (recipe.qualities_required == null || recipe.qualities_required.Count == 0)
            return true;

        IReadOnlyList<InventoryContainer> sources = pool.Sources;
        for (int q = 0; q < recipe.qualities_required.Count; q++)
        {
            QualityEntry required = recipe.qualities_required[q];
            if (required == null || string.IsNullOrEmpty(required.id))
                continue;

            bool found = false;
            for (int c = 0; c < sources.Count && !found; c++)
            {
                IReadOnlyList<ItemStack> stacks = sources[c].Stacks;
                for (int s = 0; s < stacks.Count && !found; s++)
                {
                    ItemData item = stacks[s]?.Item;
                    if (item?.qualities == null)
                        continue;

                    for (int qi = 0; qi < item.qualities.Count; qi++)
                    {
                        QualityEntry got = item.qualities[qi];
                        if (got == null || got.id != required.id)
                            continue;
                        if (got.level >= required.level)
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }

            if (!found)
            {
                ICraftingEnvironment env = CraftingEnvironment.Active;
                if (env == null || env.GetEnvQualityLevel(required.id) < required.level)
                    return false;
            }
        }

        return true;
    }

    static bool MeetsComponentSlots(
        RecipeData recipe,
        CraftingMaterialPool pool,
        IReadOnlyList<int> componentAltIndices)
    {
        if (recipe.components == null || recipe.components.Count == 0)
            return true;

        for (int i = 0; i < recipe.components.Count; i++)
        {
            if (!TryPickComponentAlt(recipe.components[i], pool, componentAltIndices, i, out _))
                return false;
        }

        return true;
    }

    static bool MeetsToolSlots(
        RecipeData recipe,
        CraftingMaterialPool pool,
        IReadOnlyList<int> toolAltIndices)
    {
        if (recipe.tools == null || recipe.tools.Count == 0)
            return true;

        for (int i = 0; i < recipe.tools.Count; i++)
        {
            if (!TryPickToolAlt(recipe.tools[i], pool, toolAltIndices, i, out _))
                return false;
        }

        return true;
    }

    static bool TryPickComponentAlt(
        ComponentSlot slot,
        CraftingMaterialPool pool,
        IReadOnlyList<int> indices,
        int slotIndex,
        out ComponentAlt chosen)
    {
        chosen = null;
        if (slot == null || slot.alternatives == null || slot.alternatives.Count == 0)
            return false;

        if (TryGetForcedIndex(indices, slotIndex, out int forcedIndex))
        {
            if (forcedIndex < 0 || forcedIndex >= slot.alternatives.Count)
                return false;

            ComponentAlt alt = slot.alternatives[forcedIndex];
            if (!IsComponentAltSatisfied(alt, pool))
                return false;

            chosen = alt;
            return true;
        }

        for (int j = 0; j < slot.alternatives.Count; j++)
        {
            ComponentAlt alt = slot.alternatives[j];
            if (!IsComponentAltSatisfied(alt, pool))
                continue;

            chosen = alt;
            return true;
        }

        return false;
    }

    static bool TryPickToolAlt(
        ToolSlot slot,
        CraftingMaterialPool pool,
        IReadOnlyList<int> indices,
        int slotIndex,
        out ToolAlt chosen)
    {
        chosen = null;
        if (slot == null || slot.alternatives == null || slot.alternatives.Count == 0)
            return false;

        if (TryGetForcedIndex(indices, slotIndex, out int forcedIndex))
        {
            if (forcedIndex < 0 || forcedIndex >= slot.alternatives.Count)
                return false;

            ToolAlt alt = slot.alternatives[forcedIndex];
            if (!IsToolAltSatisfied(alt, pool))
                return false;

            chosen = alt;
            return true;
        }

        for (int j = 0; j < slot.alternatives.Count; j++)
        {
            ToolAlt alt = slot.alternatives[j];
            if (!IsToolAltSatisfied(alt, pool))
                continue;

            chosen = alt;
            return true;
        }

        return false;
    }

    static bool IsComponentAltSatisfied(ComponentAlt alt, CraftingMaterialPool pool)
    {
        if (alt == null || string.IsNullOrEmpty(alt.item))
            return false;

        return pool.CountItem(alt.item) >= alt.count;
    }

    static bool IsToolAltSatisfied(ToolAlt alt, CraftingMaterialPool pool)
    {
        if (alt == null || string.IsNullOrEmpty(alt.tool))
            return false;

        if (IsPseudoOrEnvToolSatisfied(alt.tool, pool))
            return true;

        if (pool.CountItem(alt.tool) <= 0)
            return false;

        if (alt.charges > 0)
            return pool.CountToolCharges(alt.tool) >= alt.charges;

        return true;
    }

    static bool IsPseudoOrEnvToolSatisfied(string toolId, CraftingMaterialPool pool)
    {
        ItemData item = GameplayData.GetItem(toolId);
        bool isPseudo = HasItemFlag(item, CraftingPseudoIds.PseudoFlag);

        if (string.Equals(toolId, CraftingPseudoIds.Fire, System.StringComparison.Ordinal))
        {
            if (IsToolSatisfiedByEnvironment(CraftingPseudoIds.Fire))
                return true;
            return HasChargedHeatTool(pool);
        }

        if (isPseudo || IsKnownPseudoId(toolId))
            return IsToolSatisfiedByEnvironment(toolId);

        if (IsToolSatisfiedByEnvironment(toolId))
            return true;

        ICraftingEnvironment env = CraftingEnvironment.Active;
        return env != null && env.HasEnvTool(toolId);
    }

    static bool IsEnvOnlyTool(string toolId)
    {
        if (string.IsNullOrEmpty(toolId))
            return false;
        if (IsKnownPseudoId(toolId))
            return true;
        ItemData item = GameplayData.GetItem(toolId);
        return HasItemFlag(item, CraftingPseudoIds.PseudoFlag);
    }

    static bool IsKnownPseudoId(string toolId) =>
        string.Equals(toolId, CraftingPseudoIds.Fire, System.StringComparison.Ordinal) ||
        string.Equals(toolId, CraftingPseudoIds.Apparatus, System.StringComparison.Ordinal) ||
        string.Equals(toolId, CraftingPseudoIds.Sunlight, System.StringComparison.Ordinal);

    static bool IsToolSatisfiedByEnvironment(string toolId)
    {
        ICraftingEnvironment env = CraftingEnvironment.Active;
        return env != null && env.HasPseudoTool(toolId);
    }

    static bool HasChargedHeatTool(CraftingMaterialPool pool)
    {
        if (pool == null)
            return false;
        for (int i = 0; i < CraftingPseudoIds.HeatToolIds.Length; i++)
        {
            string id = CraftingPseudoIds.HeatToolIds[i];
            if (pool.CountItem(id) > 0 && pool.CountToolCharges(id) > 0)
                return true;
            if (pool.CountItem(id) > 0)
            {
                ItemData item = GameplayData.GetItem(id);
                if (item?.tool == null || item.tool.max_charges <= 0)
                    return true;
            }
        }

        return false;
    }

    static bool HasItemFlag(ItemData item, string flag)
    {
        if (item?.flags == null || string.IsNullOrEmpty(flag))
            return false;
        for (int i = 0; i < item.flags.Count; i++)
        {
            string value = item.flags[i];
            if (!string.IsNullOrEmpty(value) &&
                value.Equals(flag, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    static bool ShouldStampCooked(RecipeData recipe)
    {
        if (recipe == null)
            return false;
        if (recipe.hot_result)
            return true;
        if (string.Equals(recipe.skill_used, "cooking", System.StringComparison.OrdinalIgnoreCase))
            return true;
        ItemData result = GameplayData.GetItem(recipe.result);
        return result?.comestible != null;
    }

    static bool MeetsProficiencies(RecipeData recipe)
    {
        if (recipe.proficiencies == null || recipe.proficiencies.Count == 0)
            return true;

        ICharacterProficiencies profs = GameplayData.Proficiencies;
        for (int i = 0; i < recipe.proficiencies.Count; i++)
        {
            ProficiencyReq req = recipe.proficiencies[i];
            if (req == null || string.IsNullOrEmpty(req.proficiency) || !req.required)
                continue;
            if (!profs.Has(req.proficiency))
                return false;
        }

        return true;
    }

    static float ResolveProficiencyTimeMultiplier(RecipeData recipe)
    {
        if (recipe.proficiencies == null || recipe.proficiencies.Count == 0)
            return 1f;

        ICharacterProficiencies profs = GameplayData.Proficiencies;
        float mult = 1f;
        for (int i = 0; i < recipe.proficiencies.Count; i++)
        {
            ProficiencyReq req = recipe.proficiencies[i];
            if (req == null || string.IsNullOrEmpty(req.proficiency))
                continue;
            if (profs.Has(req.proficiency))
                continue;
            if (req.time_multiplier > 0f)
                mult *= req.time_multiplier;
        }

        return mult < 0.01f ? 0.01f : mult;
    }

    static void ApplyProficiencyPractice(RecipeData recipe)
    {
        if (recipe.proficiencies == null || recipe.proficiencies.Count == 0)
            return;

        ICharacterProficiencies profs = GameplayData.Proficiencies;
        int xp = recipe.difficulty * PracticeDifficultyMultiplier + PracticeDifficultyBonus;
        for (int i = 0; i < recipe.proficiencies.Count; i++)
        {
            ProficiencyReq req = recipe.proficiencies[i];
            if (req == null || string.IsNullOrEmpty(req.proficiency))
                continue;
            profs.AddPractice(req.proficiency, xp);
        }
    }

    /// <summary>Optional Gameplay bridge for activity fatigue / morale.</summary>
    public static System.Action<RecipeData> CraftCompletedSideEffects;

    static void ApplyCraftSideEffects(RecipeData recipe)
    {
        if (recipe == null)
            return;
        CraftCompletedSideEffects?.Invoke(recipe);
    }

    static bool TryGetForcedIndex(IReadOnlyList<int> indices, int slotIndex, out int forcedIndex)
    {
        if (indices != null && indices.Count > 0 && slotIndex < indices.Count)
        {
            forcedIndex = indices[slotIndex];
            return true;
        }

        forcedIndex = -1;
        return false;
    }

    static void NotifyPoolSourcesChanged(InventorySession session, CraftingMaterialPool pool)
    {
        if (session == null || pool == null)
            return;

        IReadOnlyList<InventoryContainer> sources = pool.Sources;
        if (sources.Count == 0)
        {
            session.NotifyExternalStacksChanged();
            return;
        }

        var changed = new InventoryContainer[sources.Count];
        for (int i = 0; i < sources.Count; i++)
            changed[i] = sources[i];

        session.NotifyExternalStacksChanged(changed);
    }

    static int Dice(int diceCount, int diceSides)
    {
        if (diceCount <= 0 || diceSides <= 0)
            return 0;

        int sum = 0;
        for (int i = 0; i < diceCount; i++)
            sum += Random.Range(1, diceSides + 1);

        return sum;
    }
}
