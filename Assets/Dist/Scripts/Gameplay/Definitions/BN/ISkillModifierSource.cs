// ============================================================
// ISkillModifierSource — Refresh 시 Buffed에 가산되는 수정치 소스
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public interface ISkillModifierSource
    {
        /// <summary>skillId → 가산 delta. 동일 키는 호출측에서 합산.</summary>
        void CollectModifiers(Dictionary<string, int> into);
    }
}
