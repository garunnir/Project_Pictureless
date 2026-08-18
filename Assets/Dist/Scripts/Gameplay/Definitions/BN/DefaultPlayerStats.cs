// ============================================================
// DefaultPlayerStats — IPlayerStats ??? (DefaultCharacterSkills ???)
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public sealed class DefaultPlayerStats : IPlayerStats
    {
        readonly DefaultCharacterSkills _skills;
        BodySkillModifierAggregator _bodyAggregator;

        public DefaultCharacterSkills Skills => _skills;

        public event Action<string> Changed;

        public DefaultPlayerStats()
            : this(SkillCatalog.CreateSeededSkills())
        {
        }

        public DefaultPlayerStats(DefaultCharacterSkills skills)
        {
            _skills = skills ?? SkillCatalog.CreateSeededSkills();
            _skills.Refreshed += OnSkillsRefreshed;
        }

        void OnSkillsRefreshed() => Changed?.Invoke(string.Empty);

        public int GetSkillLevel(string skillId) => _skills.Level(skillId);

        public void SetSkillLevel(string skillId, int level) => _skills.SetBaseLevel(skillId, level);

        public void AddPractice(string skillId, int xp) => _skills.AddPractice(skillId, xp);

        public int GetStat(string statKey) => _skills.Level(statKey);

        public IReadOnlyCollection<string> GetKnownSkillIds() => _skills.GetKnownSkillIds();

        public int GetPotential(string skillId) => _skills.Potential(skillId);

        public void SetPotential(string skillId, int value) => _skills.SetPotential(skillId, value);

        public void ModifyPotential(string skillId, int delta) => _skills.ModifyPotential(skillId, delta);

        public void BindBody(ICharacterBody body)
        {
            if (_bodyAggregator != null)
                _skills.RemoveModifierSource(_bodyAggregator);

            _bodyAggregator = body != null ? new BodySkillModifierAggregator(body) : null;
            if (_bodyAggregator != null)
                _skills.AddModifierSource(_bodyAggregator);

            _skills.Refresh();
        }
    }
}
