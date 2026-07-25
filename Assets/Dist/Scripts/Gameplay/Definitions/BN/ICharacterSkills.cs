// ============================================================
// ICharacterSkills — 캐릭터(플레이어·NPC) 숙련 인스턴스 계약
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public interface ICharacterSkills
    {
        /// <summary>Refresh 완료 후 1회 (UI 일괄 갱신용).</summary>
        event Action Refreshed;

        /// <summary>기본 능력치 최종값(Buffed)이 0 이하 — 스톤수프식.</summary>
        event Action Collapsed;

        bool IsCollapsed { get; }

        int Level(string skillId);
        int BaseLevel(string skillId);
        int Potential(string skillId);
        int Experience(string skillId);

        IReadOnlyCollection<string> GetKnownSkillIds();

        void SetBaseLevel(string skillId, int level);
        /// <summary>레벨 업/다운. 잠재력은 Elona식 감쇠·회복.</summary>
        void ModifyBaseLevel(string skillId, int delta);
        void AddPractice(string skillId, int baseXp);
        void SetPotential(string skillId, int value);
        void ModifyPotential(string skillId, int delta);

        void AddModifierSource(ISkillModifierSource source);
        void RemoveModifierSource(ISkillModifierSource source);

        /// <summary>Reset Buffed → 소스 가산 → 클램프 → Collapsed 검사 → Refreshed.</summary>
        void Refresh();
    }
}
