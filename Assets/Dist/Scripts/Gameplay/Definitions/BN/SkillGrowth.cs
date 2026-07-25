// ============================================================
// SkillGrowth — Elona식 숙련 성장 상수·공식 SSOT
// ============================================================

using System;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class SkillGrowth
    {
        public const int DefaultPotential = 100;
        public const int MinPotential = 1;
        public const int MaxPotential = 400;
        public const int MaxLevel = 2000;
        public const int ExperiencePerLevel = 1000;
        public const float PotentialDecayRate = 0.9f;
        public const int DefaultAttributeLevel = 8;

        public static int ClampPotential(int value) =>
            Math.Clamp(value, MinPotential, MaxPotential);

        public static int ClampLevel(int value) =>
            Math.Clamp(value, 0, MaxLevel);

        /// <summary>연습 입력 → 실제 획득 XP (Elona / OpenNefia CalcSkillExpGain).</summary>
        public static int CalcPracticeGain(int baseXp, int potential, int baseLevel)
        {
            if (baseXp <= 0 || potential <= 0)
                return 0;

            int gained = baseXp * potential / (100 + baseLevel * 15);
            return gained > 0 ? gained : 1;
        }

        public static int ApplyPotentialOnLevelDelta(int potential, int levelDelta)
        {
            int p = potential;
            if (levelDelta > 0)
            {
                for (int i = 0; i < levelDelta; i++)
                    p = ClampPotential((int)(p * PotentialDecayRate));
            }
            else if (levelDelta < 0)
            {
                int steps = -levelDelta;
                for (int i = 0; i < steps; i++)
                    p = ClampPotential((int)(p * (1f + (1f - PotentialDecayRate))) + 1);
            }

            return p;
        }
    }
}
