// ============================================================
// DefaultPlayerStats — 인메모리 기본 스킬/스탯 구현
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public sealed class DefaultPlayerStats : IPlayerStats
    {
        const int DefaultAbility = 8;
        const int SkillExerciseBase = 100;

        readonly Dictionary<string, int> _skillLevels = new(StringComparer.Ordinal);
        readonly Dictionary<string, int> _skillXp = new(StringComparer.Ordinal);
        readonly Dictionary<string, int> _stats = new(StringComparer.Ordinal)
        {
            [StatKeys.Str] = DefaultAbility,
            [StatKeys.Con] = DefaultAbility,
            [StatKeys.Dex] = DefaultAbility,
            [StatKeys.Int] = DefaultAbility,
            [StatKeys.Wis] = DefaultAbility,
            [StatKeys.Cha] = DefaultAbility
        };

        public int GetSkillLevel(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return 0;

            return _skillLevels.TryGetValue(skillId, out int lv) ? lv : 0;
        }

        public void AddPractice(string skillId, int xp)
        {
            if (string.IsNullOrEmpty(skillId) || xp <= 0)
                return;

            int level = GetSkillLevel(skillId);
            int currentXp = _skillXp.TryGetValue(skillId, out int stored) ? stored : 0;
            currentXp += xp;

            // BN 근사: required_exercise(nextLevel) = 100 × nextLevel^2
            while (true)
            {
                int nextLevel = level + 1;
                int required = RequiredXp(nextLevel);
                if (currentXp < required)
                    break;

                currentXp -= required;
                level = nextLevel;
            }

            _skillLevels[skillId] = level;
            _skillXp[skillId] = currentXp;
        }

        public int GetStat(string statKey)
        {
            if (string.IsNullOrEmpty(statKey))
                return 0;

            return _stats.TryGetValue(statKey, out int val) ? val : 0;
        }

        public IReadOnlyCollection<string> GetKnownSkillIds() => _skillLevels.Keys;

        static int RequiredXp(int level)
        {
            double v = SkillExerciseBase * level * (double)level;
            if (v > int.MaxValue)
                return int.MaxValue;
            return (int)v;
        }
    }
}
