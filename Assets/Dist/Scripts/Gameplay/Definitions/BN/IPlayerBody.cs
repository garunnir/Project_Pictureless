// ============================================================
// IPlayerBody — 신체 소유권 트리 계약 (ID 조회, 단일 노드 제거)
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public interface IPlayerBody
    {
        event Action Changed;

        IReadOnlyList<BodyPartNode> Roots { get; }

        bool TryGet(string partId, out BodyPartNode node);

        bool Has(string partId);

        /// <summary>
        /// 부모 컬렉션에서 해당 노드만 제거한다.
        /// 하위 부위·부착 효과는 소유권으로 함께 도달 불가가 된다 (연쇄 순회 삭제 없음).
        /// </summary>
        bool RemovePart(string partId);

        int GetConditionCur(string mainConditionPartId);

        int GetConditionMax(string mainConditionPartId);

        void SetCondition(string mainConditionPartId, int current, int max);

        bool IsDeadState { get; }

        void CollectEffectsUnder(string partId, List<BodyPartEffect> into, bool includeDescendants);
    }
}
