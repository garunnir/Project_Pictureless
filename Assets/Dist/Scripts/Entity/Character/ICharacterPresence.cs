// ============================================================
// ICharacterPresence — 타인 감각에 대한 본체 존재감(가시성·소음) 출력 계약
// ============================================================

/// <summary>
/// Listener 쪽 <see cref="CharacterVision"/> / <see cref="CharacterHearing"/> 과 분리.
/// NPC·청각 판정은 대상의 Presence를 소비한다.
/// </summary>
public interface ICharacterPresence
{
    /// <summary>타인 시력 판정 반경 배율 (1 = 완전 노출, 0 = 사실상 미탐지).</summary>
    float Visibility01 { get; }

    /// <summary>이동 소음 강도 (0 = 무음, 1 = 기준 소음).</summary>
    float Noise01 { get; }

    CharacterPresenceResolved Resolved { get; }
}
