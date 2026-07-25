// ============================================================
// SkillEntry — 능력치·스킬 공통 숙련 행 (레벨·잠재력·경험)
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    public sealed class SkillEntry
    {
        public BuffableStat Level { get; } = new(0);

        /// <summary>잠재력(%). 바이탈과 달리 숙련 행에만 존재.</summary>
        public int Potential { get; set; } = SkillGrowth.DefaultPotential;

        /// <summary>현재 레벨 구간 경험. Elona식: 1000당 Base 레벨 +1.</summary>
        public int Experience { get; set; }

        public SkillEntry()
        {
        }

        public SkillEntry(int baseLevel, int potential = SkillGrowth.DefaultPotential, int experience = 0)
        {
            Level.Base = baseLevel;
            Level.Buffed = baseLevel;
            Potential = potential;
            Experience = experience;
        }
    }
}
