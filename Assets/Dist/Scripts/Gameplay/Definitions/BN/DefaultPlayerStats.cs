// ============================================================
// DefaultPlayerStats — IPlayerStats 어댑터 (ICharacterSkills 래핑)
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// 기존 소비처 패리티용. 신규 코드는 <see cref="Skills"/> / <see cref="ICharacterSkills"/>를 사용.
    /// </summary>
    public sealed class DefaultPlayerStats : IPlayerStats
    {
        readonly DefaultCharacterSkills _skills;
        BodySkillModifierAggregator _bodyAggregator;

        public DefaultPlayerStats()
            : this(SkillCatalog.CreateSeededSkills())
        {
        }

        public DefaultPlayerStats(DefaultCharacterSkills skills)
        {
            _skills = skills ?? throw new ArgumentNullException(nameof(skills));
            _skills.Refreshed += OnSkillsRefreshed;
        }

        public ICharacterSkills Skills => _skills;

        public event Action<string> Changed;

        /// <summary>
        /// 신체 합산 소스를 연결하고 Refresh한다. 바디 Changed 시 재호출은 호스트 책임.
        /// </summary>
        public void BindBody(IPlayerBody body)
        {
            if (_bodyAggregator != null)
            {
                _skills.RemoveModifierSource(_bodyAggregator);
                _bodyAggregator = null;
            }

            if (body == null)
            {
                _skills.Refresh();
                return;
            }

            _bodyAggregator = new BodySkillModifierAggregator(body);
            _skills.AddModifierSource(_bodyAggregator);
            _skills.Refresh();
        }

        public int GetSkillLevel(string skillId) => _skills.Level(skillId);

        public void SetSkillLevel(string skillId, int level) =>
            _skills.SetBaseLevel(skillId, level);

        public void AddPractice(string skillId, int xp) =>
            _skills.AddPractice(skillId, xp);

        public int GetStat(string statKey) =>
            string.IsNullOrEmpty(statKey) ? 0 : _skills.Level(statKey);

        public IReadOnlyCollection<string> GetKnownSkillIds() => _skills.GetKnownSkillIds();

        public int GetPotential(string skillId) => _skills.Potential(skillId);

        public void SetPotential(string skillId, int value) =>
            _skills.SetPotential(skillId, value);

        public void ModifyPotential(string skillId, int delta) =>
            _skills.ModifyPotential(skillId, delta);

        void OnSkillsRefreshed() => Changed?.Invoke(string.Empty);
    }
}
