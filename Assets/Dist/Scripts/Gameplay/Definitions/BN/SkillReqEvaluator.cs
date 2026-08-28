// ============================================================
// SkillReqEvaluator — 스킬 요구치 순수 판정 (런타임 SSOT 무의존)
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class SkillReqEvaluator
    {
        public static int SkillLevel(IPlayerStats stats, string skillId) =>
            stats?.GetSkillLevel(skillId) ?? 0;

        public static bool MeetsCraftGate(RecipeData recipe, IPlayerStats stats)
        {
            if (recipe == null)
                return false;

            if (!string.IsNullOrEmpty(recipe.skill_used))
            {
                if (SkillLevel(stats, recipe.skill_used) < recipe.difficulty)
                    return false;
            }

            if (recipe.skills_required == null || recipe.skills_required.Count == 0)
                return true;

            for (int i = 0; i < recipe.skills_required.Count; i++)
            {
                SkillReq req = recipe.skills_required[i];
                if (req == null || string.IsNullOrEmpty(req.skill))
                    continue;

                if (SkillLevel(stats, req.skill) < req.level)
                    return false;
            }

            return true;
        }

        /// <summary>autolearn_skills / decomp_learn 등. 빈 목록 → false.</summary>
        public static bool MeetsAll(IReadOnlyList<SkillReq> reqs, IPlayerStats stats)
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
                if (SkillLevel(stats, req.skill) < req.level)
                    return false;
            }

            return sawValid;
        }

        public static string FirstUnmetReason(IReadOnlyList<SkillReq> reqs, IPlayerStats stats)
        {
            if (reqs == null)
                return Loc.Get("RecipeKnowledge.Locked");

            for (int i = 0; i < reqs.Count; i++)
            {
                SkillReq req = reqs[i];
                if (req == null || string.IsNullOrEmpty(req.skill))
                    continue;
                if (SkillLevel(stats, req.skill) < req.level)
                    return Loc.Format("RecipeKnowledge.SkillRequired", req.level);
            }

            return Loc.Get("RecipeKnowledge.Locked");
        }
    }
}
