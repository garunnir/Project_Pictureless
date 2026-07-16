// ============================================================
// DefaultPlayerStats — 인메모리 기본 스킬/스탯 구현
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public sealed class DefaultPlayerStats : IPlayerStats
    {
        const int DefaultInt = 8;
    const int SkillExerciseBase = 100;

        readonly Dictionary<string, int> _skillLevels = new(StringComparer.Ordinal);
        readonly Dictionary<string, int> _skillXp = new(StringComparer.Ordinal);
        readonly Dictionary<string, int> _stats = new(StringComparer.Ordinal)
        {
            [StatKeys.Int] = DefaultInt
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

            // BN 기준(근사):
            // required_exercise(nextLevel) = 100 × nextLevel^2
            // 누적 xp가 임계를 넘으면 레벨업하고 사용한 xp는 차감.
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

        static int RequiredXp(int level)
        {
            // level: 다음 레벨(= 현재 level+1)
            // BN 기준: 100 * (current_level+1)^2
            double v = SkillExerciseBase * level * (double)level;
            if (v > int.MaxValue) return int.MaxValue;
            return (int)v;
        }
    }
}

