// ============================================================
// IPlayerStats — 플레이어 스킬·스탯 조회와 변경 통지 계약
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public interface IPlayerStats
    {
        /// <summary>스킬 값이 변경되면 해당 skillId를 전달합니다.</summary>
        event System.Action<string> Changed;

        int GetSkillLevel(string skillId);
        /// <summary>레벨을 0 이상으로 설정하며 누적 연습 XP는 유지합니다.</summary>
        void SetSkillLevel(string skillId, int level);
        void AddPractice(string skillId, int xp);
        int GetStat(string statKey);
        IReadOnlyCollection<string> GetKnownSkillIds();
    }
}
