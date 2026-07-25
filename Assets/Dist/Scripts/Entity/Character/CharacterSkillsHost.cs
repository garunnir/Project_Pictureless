// ============================================================
// CharacterSkillsHost — 캐릭터(플레이어·NPC) 숙련 인스턴스 보유 컴포넌트
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterSkillsHost : MonoBehaviour
{
    DefaultCharacterSkills _skills;
    ICharacterDefeat _defeat;

    /// <summary>지연 생성. 카탈로그(skills.json) 시드, 없으면 코드 기본값.</summary>
    public ICharacterSkills Skills
    {
        get
        {
            if (_skills == null)
                _skills = SkillCatalog.CreateSeededSkills();
            return _skills;
        }
    }

    /// <summary>
    /// 최종 사망/패배 판정. NPC 바디 모델 부재로 현재는 스탯 붕괴만 입력된다
    /// (바디 연결 시 생성자 인자에 IPlayerBody 추가).
    /// </summary>
    public ICharacterDefeat Defeat
    {
        get
        {
            if (_defeat == null)
                _defeat = new DefaultCharacterDefeat(null, Skills);
            return _defeat;
        }
    }
}
