// ============================================================
// RecipeKnowledge — autolearn / book_learn / decomp_learn 해금 판단
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class RecipeKnowledge
    {
        /// <summary>null이면 해금됨. memory 생략 시 GameplayPlayerRuntime.RecipeMemory.</summary>
        public static string GetFailureReason(RecipeData recipe, IItemContainer container)
            => GetFailureReason(recipe, container, GameplayPlayerRuntime.RecipeMemory);

        public static string GetFailureReason(
            RecipeData recipe,
            IItemContainer container,
            ICharacterRecipeMemory memory)
            => GetFailureReason(
                recipe,
                container,
                memory,
                GameplayPlayerRuntime.Stats,
                GameplayPlayerRuntime.Traits);

        public static string GetFailureReason(
            RecipeData recipe,
            IItemContainer container,
            ICharacterRecipeMemory memory,
            IPlayerStats stats,
            ICharacterTraits traits)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.result))
                return Loc.Get("RecipeKnowledge.Invalid");

            if (traits != null && traits.Has(TraitIds.Omniscience))
                return null;

            if (memory != null && !string.IsNullOrEmpty(recipe.id) && memory.IsKnown(recipe.id))
                return null;

            int playerSkillLv = !string.IsNullOrEmpty(recipe.skill_used)
                ? SkillReqEvaluator.SkillLevel(stats, recipe.skill_used)
                : 0;

            bool hasAutoSkills = HasSkillReqs(recipe.autolearn_skills);
            bool hasAuto = recipe.autolearn;
            bool hasBooks = recipe.book_learn != null && recipe.book_learn.Count > 0;
            bool hasDecomp = HasSkillReqs(recipe.decomp_learn);

            if (hasAutoSkills && SkillReqEvaluator.MeetsAll(recipe.autolearn_skills, stats))
                return null;

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

            if (!hasAuto && !hasBooks && !hasDecomp && !hasAutoSkills)
                return null;

            if (hasDecomp && !hasAuto && !hasBooks && !hasAutoSkills)
                return Loc.Get("RecipeKnowledge.DecompRequired");

            if (hasAutoSkills)
                return SkillReqEvaluator.FirstUnmetReason(recipe.autolearn_skills, stats);

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
            ICharacterRecipeMemory memory,
            IPlayerStats stats)
        {
            if (recipe == null || memory == null || string.IsNullOrEmpty(recipe.id))
                return;

            if (HasSkillReqs(recipe.decomp_learn) && SkillReqEvaluator.MeetsAll(recipe.decomp_learn, stats))
                memory.Learn(recipe.id);

            if (recipe.is_uncraft || !HasSkillReqs(recipe.decomp_learn))
            {
                List<RecipeData> forwards = GameDataQueries.GetRecipesForResult(recipe.result);
                if (forwards == null)
                    return;

                for (int i = 0; i < forwards.Count; i++)
                {
                    RecipeData forward = forwards[i];
                    if (forward == null || forward.is_uncraft || string.IsNullOrEmpty(forward.id))
                        continue;
                    if (!HasSkillReqs(forward.decomp_learn))
                        continue;
                    if (!SkillReqEvaluator.MeetsAll(forward.decomp_learn, stats))
                        continue;
                    memory.Learn(forward.id);
                }
            }
        }

        public static void TryLearnFromDisassembly(
            RecipeData recipe,
            ICharacterRecipeMemory memory)
            => TryLearnFromDisassembly(recipe, memory, GameplayPlayerRuntime.Stats);

        public static bool MeetsCraftSkillRequirements(RecipeData recipe, IPlayerStats stats) =>
            SkillReqEvaluator.MeetsCraftGate(recipe, stats);

        public static bool MeetsCraftSkillRequirements(RecipeData recipe) =>
            MeetsCraftSkillRequirements(recipe, GameplayPlayerRuntime.Stats);

        public static bool MeetsAllSkillReqs(IReadOnlyList<SkillReq> reqs, IPlayerStats stats) =>
            SkillReqEvaluator.MeetsAll(reqs, stats);

        public static bool MeetsAllSkillReqs(IReadOnlyList<SkillReq> reqs) =>
            SkillReqEvaluator.MeetsAll(reqs, GameplayPlayerRuntime.Stats);

        static bool HasSkillReqs(IReadOnlyList<SkillReq> reqs)
            => reqs != null && reqs.Count > 0;

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

                ItemData bookItem = GameDataQueries.GetItem(bl.book);
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
