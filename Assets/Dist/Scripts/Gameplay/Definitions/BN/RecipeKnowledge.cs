// ============================================================
// RecipeKnowledge — autolearn / book_learn 기반 레시피 해금 판단
// ============================================================

using System;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class RecipeKnowledge
    {
        /// <summary>
        /// null이면 해금됨.
        /// </summary>
        public static string GetFailureReason(RecipeData recipe, IItemContainer container)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.result))
                return Loc.Get("RecipeKnowledge.Invalid", "Invalid recipe");

            int playerSkillLv = !string.IsNullOrEmpty(recipe.skill_used)
                ? GameplayData.Stats.GetSkillLevel(recipe.skill_used)
                : 0;

            bool hasAuto = recipe.autolearn;
            bool hasBooks = recipe.book_learn != null && recipe.book_learn.Count > 0;

            // GameData 커스텀 등: 습득 플래그가 없으면 이미 아는 레시피로 취급
            if (!hasAuto && !hasBooks)
                return null;

            if (hasAuto && playerSkillLv >= recipe.difficulty)
                return null;

            if (hasBooks)
            {
                string bookReason = TryResolveBookReason(recipe, container, playerSkillLv);
                if (bookReason == null)
                    return null;
                // 책 조건이 유일한 경로면 책 사유를 반환
                if (!hasAuto)
                    return bookReason;
            }

            if (hasAuto)
                return Loc.Format("RecipeKnowledge.SkillRequired", "스킬 lv{0} 필요", recipe.difficulty);

            return Loc.Get("RecipeKnowledge.Locked", "Locked");
        }

        static string TryResolveBookReason(RecipeData recipe, IItemContainer container, int playerSkillLv)
        {
            if (container == null || recipe.book_learn == null)
                return Loc.Get("RecipeKnowledge.BookRequired", "책 필요");

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

                return Loc.Format("RecipeKnowledge.SkillRequired", "스킬 lv{0} 필요", requiredSkillLevel);
            }

            return sawBookEntry
                ? Loc.Get("RecipeKnowledge.BookRequired", "책 필요")
                : Loc.Get("RecipeKnowledge.Locked", "Locked");
        }
    }
}
