// ============================================================
// CraftingService — 합성 가능 여부 확인 + 재료 소비 + 결과 생성
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using Garunnir.Runtime.Gameplay.Data;

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
        if (recipe == null || container == null || string.IsNullOrEmpty(recipe.result))
            return false;

        // skill_used/difficulty 게이팅
        if (!string.IsNullOrEmpty(recipe.skill_used))
        {
            int lv = GameplayData.Stats.GetSkillLevel(recipe.skill_used);
            if (lv < recipe.difficulty)
                return false;
        }

        // qualities_required 게이팅
        if (recipe.qualities_required != null && recipe.qualities_required.Count > 0)
        {
            foreach (QualityEntry required in recipe.qualities_required)
            {
                if (required == null || string.IsNullOrEmpty(required.id))
                    continue;

                bool found = false;
                for (int s = 0; s < container.Stacks.Count && !found; s++)
                {
                    ItemStack stack = container.Stacks[s];
                    ItemData item = stack?.Item;
                    if (item == null || item.qualities == null)
                        continue;

                    for (int q = 0; q < item.qualities.Count; q++)
                    {
                        QualityEntry got = item.qualities[q];
                        if (got == null || got.id != required.id)
                            continue;
                        if (got.level >= required.level)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                    return false;
            }
        }

        // tools 슬롯 게이팅(충전/소비는 미구현 → 존재 여부만 검사)
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

        // components는 마지막에 검사(없으면 tool/quality/skill만으로 제작 가능)
        if (recipe.components == null || recipe.components.Count == 0)
            return true;

        for (int i = 0; i < recipe.components.Count; i++)
        {
            ComponentSlot slot = recipe.components[i];
            if (slot == null || slot.alternatives == null || slot.alternatives.Count == 0)
                return false;

            bool slotSatisfied = false;
            for (int j = 0; j < slot.alternatives.Count; j++)
            {
                ComponentAlt alt = slot.alternatives[j];
                if (alt == null || string.IsNullOrEmpty(alt.item))
                    continue;

                if (container.CountItem(alt.item) >= alt.count)
                {
                    slotSatisfied = true;
                    break;
                }
            }

            if (!slotSatisfied)
                return false;
        }

        return true;
    }

    public static bool TryCraft(
        RecipeData recipe,
        InventoryContainer container,
        InventorySession session)
    {
        if (!CanCraft(recipe, container))
            return false;

        // Consume components (tools/qualities consumption은 미구현)
        if (recipe.components != null)
        {
            for (int i = 0; i < recipe.components.Count; i++)
            {
                ComponentSlot slot = recipe.components[i];
                if (slot == null || slot.alternatives == null)
                    continue;

                for (int j = 0; j < slot.alternatives.Count; j++)
                {
                    ComponentAlt alt = slot.alternatives[j];
                    if (alt == null || string.IsNullOrEmpty(alt.item))
                        continue;

                    if (container.CountItem(alt.item) >= alt.count)
                    {
                        container.RemoveItem(alt.item, alt.count);
                        break;
                    }
                }
            }
        }

        // Result
        int resultCount = recipe.result_count > 0 ? recipe.result_count : 1;
        container.AddItem(recipe.result, resultCount);

        // Byproducts (BN의 byproducts)
        if (recipe.byproducts != null && recipe.byproducts.Count > 0)
        {
            for (int i = 0; i < recipe.byproducts.Count; i++)
            {
                Byproduct bp = recipe.byproducts[i];
                if (bp == null || string.IsNullOrEmpty(bp.item) || bp.count <= 0)
                    continue;
                container.AddItem(bp.item, bp.count);
            }
        }

        // Practice gain
        if (!string.IsNullOrEmpty(recipe.skill_used))
        {
            // BN은 난이도/시간/배치에 따라 연습량을 정규화하지만,
            // 현재 시스템에서는 난이도 기반의 단순 스케일로 시작한다.
            int practiceXp = recipe.difficulty * PracticeDifficultyMultiplier + PracticeDifficultyBonus;
            GameplayData.Stats.AddPractice(recipe.skill_used, practiceXp);
        }

        session?.NotifyExternalStacksChanged();
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

        session?.NotifyExternalStacksChanged();
        return true;
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
