// ============================================================
// SkillKind / SkillDef — 숙련 프로토타입 DTO (JSON 카탈로그용)
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    public enum SkillKind
    {
        Skill = 0,
        Attribute = 1,
        Trait = 2
    }

    /// <summary>StreamingAssets JSON 행. 로더 연결은 후속.</summary>
    [System.Serializable]
    public sealed class SkillDef
    {
        public string id;
        public string kind;
        public int initial_level;
        public int initial_potential = SkillGrowth.DefaultPotential;

        public SkillKind ParsedKind
        {
            get
            {
                if (string.Equals(kind, "attribute", System.StringComparison.OrdinalIgnoreCase))
                    return SkillKind.Attribute;
                if (string.Equals(kind, "trait", System.StringComparison.OrdinalIgnoreCase))
                    return SkillKind.Trait;
                return SkillKind.Skill;
            }
        }
    }

    [System.Serializable]
    public sealed class SkillsFileRoot
    {
        public SkillDef[] skills;
    }
}
