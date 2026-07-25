// ============================================================
// DefaultPlayerStats ??IPlayerStats ?´ëŒ‘??(ICharacterSkills ?˜í•‘)
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// ê¸°ì¡´ ?Œë¹„ì²??¨ë¦¬?°ìš©. ? ê·œ ì½”ë“œ??<see cref="Skills"/> / <see cref="ICharacterSkills"/>ë¥??¬ìš©.
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
        /// ? ì²´ ?©ì‚° ?ŒìŠ¤ë¥??°ê²°?˜ê³  Refresh?œë‹¤. ë°”ë”” Changed ???¬í˜¸ì¶œì? ?¸ìŠ¤??ì±…ìž„.
        /// </summary>
        public void BindBody(ICharacterBody body)
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
