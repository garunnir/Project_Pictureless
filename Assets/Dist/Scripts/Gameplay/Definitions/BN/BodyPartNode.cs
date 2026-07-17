// ============================================================
// BodyPartNode — 소유권 트리 노드 (자식·효과를 소유, 부모 제거 시 함께 소멸)
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public sealed class BodyPartNode
    {
        readonly List<BodyPartNode> _children = new();
        readonly List<BodyPartEffect> _effects = new();

        public string PartId { get; }
        public bool HoldsHp { get; }
        public int HpCur { get; private set; }
        public int HpMax { get; private set; }

        public IReadOnlyList<BodyPartNode> Children => _children;
        public IReadOnlyList<BodyPartEffect> Effects => _effects;

        public BodyPartNode(string partId, bool holdsHp, int hpMax = 0)
        {
            PartId = partId;
            HoldsHp = holdsHp;
            if (holdsHp)
            {
                HpMax = hpMax;
                HpCur = hpMax;
            }
        }

        public void SetHp(int current, int max)
        {
            if (!HoldsHp)
                return;

            HpMax = max < 0 ? 0 : max;
            HpCur = current < 0 ? 0 : (current > HpMax ? HpMax : current);
        }

        public void AddChild(BodyPartNode child)
        {
            if (child == null)
                return;

            _children.Add(child);
        }

        /// <summary>직계 자식만 제거. 하위 서브트리는 이 노드와 함께 도달 불가가 된다.</summary>
        public bool RemoveDirectChild(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return false;

            for (int i = 0; i < _children.Count; i++)
            {
                if (_children[i].PartId != partId)
                    continue;

                _children.RemoveAt(i);
                return true;
            }

            return false;
        }

        public void AddEffect(BodyPartEffect effect)
        {
            if (string.IsNullOrEmpty(effect.EffectId))
                return;

            _effects.Add(effect);
        }

        public void ClearEffects() => _effects.Clear();
    }
}
