// ============================================================
// DefaultCharacterSkills — 단일 숙련 테이블 + Refresh 구현
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public sealed class DefaultCharacterSkills : ICharacterSkills
    {
        readonly Dictionary<string, SkillEntry> _entries = new(StringComparer.Ordinal);
        readonly List<ISkillModifierSource> _sources = new(4);
        readonly Dictionary<string, int> _modifierScratch = new(StringComparer.Ordinal);

        public event Action Refreshed;
        public event Action Collapsed;

        public bool IsCollapsed { get; private set; }

        public static DefaultCharacterSkills CreateWithDefaultAttributes(
            int attributeLevel = SkillGrowth.DefaultAttributeLevel)
        {
            var skills = new DefaultCharacterSkills();
            for (int i = 0; i < AttributeIds.All.Length; i++)
                skills.SeedEntry(AttributeIds.All[i], attributeLevel);

            skills.Refresh();
            return skills;
        }

        /// <summary>Refresh·이벤트 없이 행을 시드한다. 시드 완료 후 Refresh 1회는 호출측 책임.</summary>
        public void SeedEntry(
            string skillId, int baseLevel, int potential = SkillGrowth.DefaultPotential)
        {
            if (string.IsNullOrEmpty(skillId))
                return;

            _entries[skillId] = new SkillEntry(
                SkillGrowth.ClampLevel(baseLevel),
                SkillGrowth.ClampPotential(potential));
        }

        public int Level(string skillId)
        {
            if (!TryGet(skillId, out SkillEntry entry))
                return 0;
            return entry.Level.Buffed;
        }

        public int BaseLevel(string skillId)
        {
            if (!TryGet(skillId, out SkillEntry entry))
                return 0;
            return entry.Level.Base;
        }

        public int Potential(string skillId)
        {
            if (!TryGet(skillId, out SkillEntry entry))
                return SkillGrowth.DefaultPotential;
            return entry.Potential;
        }

        public int Experience(string skillId)
        {
            if (!TryGet(skillId, out SkillEntry entry))
                return 0;
            return entry.Experience;
        }

        public IReadOnlyCollection<string> GetKnownSkillIds()
        {
            var list = new List<string>(_entries.Count);
            foreach (KeyValuePair<string, SkillEntry> pair in _entries)
            {
                if (AttributeIds.IsAttribute(pair.Key))
                    continue;
                if (pair.Value.Level.Base > 0 || pair.Value.Level.Buffed > 0)
                    list.Add(pair.Key);
            }

            return list;
        }

        public void SetBaseLevel(string skillId, int level)
        {
            if (string.IsNullOrEmpty(skillId))
                return;

            SkillEntry entry = EnsureEntry(skillId);
            entry.Level.Base = SkillGrowth.ClampLevel(level);
            Refresh();
        }

        public void ModifyBaseLevel(string skillId, int delta)
        {
            if (string.IsNullOrEmpty(skillId) || delta == 0)
                return;

            SkillEntry entry = EnsureEntry(skillId);
            int newBase = SkillGrowth.ClampLevel(entry.Level.Base + delta);
            int applied = newBase - entry.Level.Base;
            if (applied == 0)
                return;

            entry.Level.Base = newBase;
            entry.Potential = SkillGrowth.ApplyPotentialOnLevelDelta(entry.Potential, applied);
            Refresh();
        }

        public void AddPractice(string skillId, int baseXp)
        {
            if (string.IsNullOrEmpty(skillId) || baseXp <= 0)
                return;

            SkillEntry entry = EnsureEntry(skillId);
            int gained = SkillGrowth.CalcPracticeGain(baseXp, entry.Potential, entry.Level.Base);
            if (gained <= 0)
                return;

            ApplyExperienceDelta(entry, gained);
            Refresh();
        }

        public void SetPotential(string skillId, int value)
        {
            if (string.IsNullOrEmpty(skillId))
                return;

            SkillEntry entry = EnsureEntry(skillId);
            entry.Potential = SkillGrowth.ClampPotential(value);
            Refresh();
        }

        public void ModifyPotential(string skillId, int delta)
        {
            if (string.IsNullOrEmpty(skillId) || delta == 0)
                return;

            SkillEntry entry = EnsureEntry(skillId);
            entry.Potential = SkillGrowth.ClampPotential(entry.Potential + delta);
            Refresh();
        }

        public void AddModifierSource(ISkillModifierSource source)
        {
            if (source == null || _sources.Contains(source))
                return;
            _sources.Add(source);
        }

        public void RemoveModifierSource(ISkillModifierSource source)
        {
            if (source == null)
                return;
            _sources.Remove(source);
        }

        public void Refresh()
        {
            foreach (KeyValuePair<string, SkillEntry> pair in _entries)
                pair.Value.Level.Reset();

            _modifierScratch.Clear();
            for (int i = 0; i < _sources.Count; i++)
                _sources[i]?.CollectModifiers(_modifierScratch);

            foreach (KeyValuePair<string, int> mod in _modifierScratch)
            {
                if (mod.Value == 0 || string.IsNullOrEmpty(mod.Key))
                    continue;

                SkillEntry entry = EnsureEntry(mod.Key);
                entry.Level.Buffed = entry.Level.Buffed + mod.Value;
            }

            foreach (KeyValuePair<string, SkillEntry> pair in _entries)
            {
                int buffed = pair.Value.Level.Buffed;
                if (buffed < 0)
                    pair.Value.Level.Buffed = 0;
                else if (buffed > SkillGrowth.MaxLevel)
                    pair.Value.Level.Buffed = SkillGrowth.MaxLevel;
            }

            bool collapsed = false;
            for (int i = 0; i < AttributeIds.All.Length; i++)
            {
                string id = AttributeIds.All[i];
                if (Level(id) <= 0)
                {
                    collapsed = true;
                    break;
                }
            }

            bool wasCollapsed = IsCollapsed;
            IsCollapsed = collapsed;
            if (collapsed && !wasCollapsed)
                Collapsed?.Invoke();

            Refreshed?.Invoke();
        }

        void ApplyExperienceDelta(SkillEntry entry, int expDelta)
        {
            long newExp = (long)entry.Experience + expDelta;

            if (newExp >= SkillGrowth.ExperiencePerLevel)
            {
                int levelDelta = (int)(newExp / SkillGrowth.ExperiencePerLevel);
                newExp %= SkillGrowth.ExperiencePerLevel;
                int newLevel = SkillGrowth.ClampLevel(entry.Level.Base + levelDelta);
                int applied = newLevel - entry.Level.Base;
                entry.Level.Base = newLevel;
                if (applied != 0)
                    entry.Potential = SkillGrowth.ApplyPotentialOnLevelDelta(entry.Potential, applied);
                entry.Experience = (int)newExp;
            }
            else if (newExp < 0)
            {
                // Elona: 음수 경험으로 레벨 다운
                int levelDelta = (int)((-newExp) / SkillGrowth.ExperiencePerLevel) + 1;
                newExp = SkillGrowth.ExperiencePerLevel + (newExp % SkillGrowth.ExperiencePerLevel);
                int newLevel = SkillGrowth.ClampLevel(entry.Level.Base - levelDelta);
                int applied = newLevel - entry.Level.Base;
                entry.Level.Base = newLevel;
                if (applied != 0)
                    entry.Potential = SkillGrowth.ApplyPotentialOnLevelDelta(entry.Potential, applied);
                entry.Experience = newLevel <= 0 ? 0 : (int)Math.Max(0, newExp);
            }
            else
            {
                entry.Experience = (int)newExp;
            }
        }

        SkillEntry EnsureEntry(string skillId)
        {
            if (_entries.TryGetValue(skillId, out SkillEntry entry))
                return entry;

            entry = new SkillEntry(0);
            _entries[skillId] = entry;
            return entry;
        }

        bool TryGet(string skillId, out SkillEntry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(skillId))
                return false;
            return _entries.TryGetValue(skillId, out entry);
        }
    }
}
