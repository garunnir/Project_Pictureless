// ============================================================
// ICharacterBody — 신체 소유권 트리 계약 (플레이어·NPC 공용)
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public interface ICharacterBody
    {
        event Action Changed;

        IReadOnlyList<BodyPartNode> Roots { get; }

        bool TryGet(string partId, out BodyPartNode node);

        bool Has(string partId);

        /// <summary>
        /// 부모 컬렉션에서 해당 노드만 제거한다.
        /// 하위 부위·부착 효과는 소유권으로 함께 도달 불가가 된다.
        /// 소켓(부모)은 남으므로 <see cref="TryAttach"/>로 다시 채울 수 있다.
        /// </summary>
        bool RemovePart(string partId);

        /// <summary>
        /// 런타임 복원 전용 AddChild 경로. parentId가 비면 루트로 붙인다.
        /// 이미 같은 partId가 있으면 false.
        /// </summary>
        bool TryAttach(string parentId, BodyPartNode node);

        int GetConditionCur(string mainConditionPartId);

        int GetConditionMax(string mainConditionPartId);

        void SetCondition(string mainConditionPartId, int current, int max);

        bool AddEffect(string partId, BodyPartEffect effect);

        bool EnsureEffectMinIntensity(
            string partId,
            string effectId,
            int intensity,
            float remainingSeconds = -1f);

        bool ReduceEffectIntensity(string partId, string effectId, int reduceBy);

        bool ClearEffectsOn(string partId);

        /// <summary>유한 효과 RemainingSeconds를 줄이고 만료분을 제거. 변경 시 true.</summary>
        bool TickEffectDurations(float deltaSeconds);

        bool IsDeadState { get; }

        float Blood01 { get; }
        float Toxin01 { get; }
        float InfectionProgress01 { get; }
        float InfectionImmunity01 { get; }

        void SetBlood01(float value);
        void SetToxin01(float value);
        void SetInfectionProgress01(float value);
        void SetInfectionImmunity01(float value);

        void CollectEffectsUnder(string partId, List<BodyPartEffect> into, bool includeDescendants);

        CharacterBodyDto ToDto();

        void FromDto(CharacterBodyDto dto);
    }
}
