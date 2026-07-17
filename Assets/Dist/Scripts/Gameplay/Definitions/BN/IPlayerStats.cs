// ============================================================
// IPlayerStats — 제작/분해 로직이 의존하는 플레이어 스킬/스탯 계약
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public interface IPlayerStats
    {
        int GetSkillLevel(string skillId);
        void AddPractice(string skillId, int xp);
        int GetStat(string statKey);
        IReadOnlyCollection<string> GetKnownSkillIds();
    }
}
