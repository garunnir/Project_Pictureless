// ============================================================
// ICharacterDefeat — 최종 사망/패배 판정 레이어 (Body·Skills OR)
// ============================================================

using System;

namespace Garunnir.Runtime.Gameplay.Data
{
    public enum DefeatCause
    {
        None = 0,
        /// <summary>해부학적 치명 (의식 0 — 뇌/피/감염/고통1/독소).</summary>
        BodyFatal = 1,
        /// <summary>기본 능력치 최종값(Buffed) 0 — 스톤수프식. 흔하면 안 됨 — 래치 시 [StatCollapse] 로그.</summary>
        StatCollapse = 2
    }

    /// <summary>
    /// 소비처(턴·AI·UI)는 <see cref="IsDefeated"/>만 본다.
    /// 판정만 수행하며 Body/Skills를 수정하지 않는다.
    /// </summary>
    public interface ICharacterDefeat
    {
        event Action Changed;

        bool IsDefeated { get; }
        DefeatCause Cause { get; }

        /// <summary>부활/디버그용. 래치를 해제하고 즉시 재평가한다.</summary>
        void Revive();
    }
}
