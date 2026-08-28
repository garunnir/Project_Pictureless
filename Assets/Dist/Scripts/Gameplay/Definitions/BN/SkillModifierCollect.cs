// ============================================================
// SkillModifierCollect — ISkillModifierSource 수집 버퍼 헬퍼
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class SkillModifierCollect
    {
        public static void AddDelta(Dictionary<string, int> into, string skillId, int delta)
        {
            if (delta == 0 || into == null)
                return;

            if (into.TryGetValue(skillId, out int existing))
                into[skillId] = existing + delta;
            else
                into[skillId] = delta;
        }
    }
}
