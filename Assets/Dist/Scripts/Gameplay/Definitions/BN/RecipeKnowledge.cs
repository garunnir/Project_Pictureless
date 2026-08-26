// ============================================================
// RecipeKnowledge — autolearn / book_learn / decomp_learn 해금 판단
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class RecipeKnowledge
    {
        /// <summary>
        /// null이면 해금됨. memory 생략 시 GameplayData.RecipeMemory.
        /// </summary>
        public static string GetFailureReason(RecipeData recipe, IItemContainer container)
            => GetFailureReason(recipe, container, GameplayData.RecipeMemory);

        public static string GetFailureReason(
            RecipeData recipe,
            IItemContainer container,
            ICharacterRecipeMemory memory)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.result))
                return Loc.Get("RecipeKnowledge.Invalid");

            // 전지: 모든 레시피 Known (재료·CanCraft와 무관)
            if (GameplayData.Traits != null && GameplayData.Traits.Has(TraitIds.Omniscience))
                return null;

            if (memory != null && !string.IsNullOrEmpty(recipe.id) && memory.IsKnown(recipe.id))
                return null;

            int playerSkillLv = !string.IsNullOrEmpty(recipe.skill_used)
                ? GameplayData.Stats.GetSkillLevel(recipe.skill_used)
                : 0;

            bool hasAutoSkills = HasSkillReqs(recipe.autolearn_skills);
            bool hasAuto = recipe.autolearn;
            bool hasBooks = recipe.book_learn != null && recipe.book_learn.Count > 0;
            bool hasDecomp = HasSkillReqs(recipe.decomp_learn);

            if (hasAutoSkills && MeetsAllSkillReqs(recipe.autolearn_skills))
                return null;

            // autolearn_skills가 있으면 difficulty 단독 경로는 쓰지 않음 (BN array autolearn)
            if (hasAuto && !hasAutoSkills && playerSkillLv >= recipe.difficulty)
                return null;

            if (hasBooks)
            {
                string bookReason = TryResolveBookReason(recipe, container, playerSkillLv);
                if (bookReason == null)
                    return null;
                if (!hasAuto && !hasAutoSkills && !hasDecomp)
                    return bookReason;
            }

            // GameData 커스텀 등: 습득 플래그가 없으면 이미 아는 레시피
            if (!hasAuto && !hasBooks && !hasDecomp && !hasAutoSkills)
                return null;

            if (hasDecomp && !hasAuto && !hasBooks && !hasAutoSkills)
                return Loc.Get("RecipeKnowledge.DecompRequired");

            if (hasAutoSkills)
            {
                string unmet = FirstUnmetSkillReason(recipe.autolearn_skills);
                if (unmet != null)
                    return unmet;
            }

            if (hasAuto && !hasAutoSkills)
                return Loc.Format("RecipeKnowledge.SkillRequired", recipe.difficulty);

            if (hasBooks)
                return TryResolveBookReason(recipe, container, playerSkillLv);

            if (hasDecomp)
                return Loc.Get("RecipeKnowledge.DecompRequired");

            return Loc.Get("RecipeKnowledge.Locked");
        }

        /// <summary>decomp_learn 스킬 충족 시 영구 습득. forward recipe id 기준.</summary>
        public static void TryLearnFromDisassembly(
            RecipeData recipe,
            ICharacterRecipeMemory memory)
        {
            if (recipe == null || memory == null || string.IsNullOrEmpty(recipe.id))
                return;

            if (HasSkillReqs(recipe.decomp_learn) && MeetsAllSkillReqs(recipe.decomp_learn))
                memory.Learn(recipe.id);

            // uncraft-only 행이면 같은 result의 forward 레시피 decomp_learn도 검사
            if (recipe.is_uncraft || !HasSkillReqs(recipe.decomp_learn))
            {
                List<RecipeData> forwards = GameplayData.GetRecipesForResult(recipe.result);
                if (forwards == null)
                    return;

                for (int i = 0; i < forwards.Count; i++)
                {
                    RecipeData forward = forwards[i];
                    if (forward == null || forward.is_uncraft || string.IsNullOrEmpty(forward.id))
                        continue;
                    if (!HasSkillReqs(forward.decomp_learn))
                        continue;
                    if (!MeetsAllSkillReqs(forward.decomp_learn))
                        continue;
                    memory.Learn(forward.id);
                }
            }
        }

        public static bool MeetsAllSkillReqs(IReadOnlyList<SkillReq> reqs)
        {
            if (reqs == null || reqs.Count == 0)
                return false;

            bool sawValid = false;
            for (int i = 0; i < reqs.Count; i++)
            {
                SkillReq req = reqs[i];
                if (req == null || string.IsNullOrEmpty(req.skill))
                    continue;

                sawValid = true;
                if (GameplayData.Stats.GetSkillLevel(req.skill) < req.level)
                    return false;
            }

            return sawValid;
        }

        static bool HasSkillReqs(IReadOnlyList<SkillReq> reqs)
            => reqs != null && reqs.Count > 0;

        static string FirstUnmetSkillReason(IReadOnlyList<SkillReq> reqs)
        {
            if (reqs == null)
                return Loc.Get("RecipeKnowledge.Locked");

            for (int i = 0; i < reqs.Count; i++)
            {
                SkillReq req = reqs[i];
                if (req == null || string.IsNullOrEmpty(req.skill))
                    continue;
                if (GameplayData.Stats.GetSkillLevel(req.skill) < req.level)
                    return Loc.Format("RecipeKnowledge.SkillRequired", req.level);
            }

            return Loc.Get("RecipeKnowledge.Locked");
        }

        static string TryResolveBookReason(RecipeData recipe, IItemContainer container, int playerSkillLv)
        {
            if (container == null || recipe.book_learn == null)
                return Loc.Get("RecipeKnowledge.BookRequired");

            bool sawBookEntry = false;

            for (int i = 0; i < recipe.book_learn.Count; i++)
            {
                BookLearn bl = recipe.book_learn[i];
                if (bl == null || string.IsNullOrEmpty(bl.book))
                    continue;

                sawBookEntry = true;

                if (container.CountItem(bl.book) <= 0)
                    continue;

                ItemData bookItem = GameplayData.GetItem(bl.book);
                int requiredByRecipe = bl.level;
                int requiredByBook = bookItem != null ? bookItem.book_required_level : 0;
                int requiredSkillLevel = Math.Max(requiredByRecipe, requiredByBook);

                if (playerSkillLv >= requiredSkillLevel)
                    return null;

                return Loc.Format("RecipeKnowledge.SkillRequired", requiredSkillLevel);
            }

            return sawBookEntry
                ? Loc.Get("RecipeKnowledge.BookRequired")
                : Loc.Get("RecipeKnowledge.Locked");
        }
    }
}
