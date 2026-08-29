// ============================================================
// TraitIds — 캐릭터 상시 패시브(특성) ID SSOT
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// Dist 특성(상시 패시브). 전투 Hit bash/cut/bullet "특성"과 다름.
    /// </summary>
    public static class TraitIds
    {
        /// <summary>생존술 — 바이탈 수치 표시 등.</summary>
        public const string Survival = "survival";

        /// <summary>전지 — 모든 레시피 습득.</summary>
        public const string Omniscience = "omniscience";

        /// <summary>만시 — 캐릭터 시야 페이드 사라짐·청각 핑 무효.</summary>
        public const string Omnivision = "omnivision";

        public static readonly string[] All =
        {
            Survival,
            Omniscience,
            Omnivision
        };

        public static bool IsTrait(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            for (int i = 0; i < All.Length; i++)
            {
                if (All[i] == id)
                    return true;
            }

            return false;
        }
    }
}
