// ============================================================
// BodyPartNode — 소유권 트리 노드 (자식·효과를 소유, 부모 제거 시 함께 소멸)
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public enum BodyPartKind
    {
        Organic = 0,
        Prosthetic = 1
    }

    public sealed class BodyPartNode
    {
        readonly List<BodyPartNode> _children = new();
        readonly List<BodyPartEffect> _effects = new();

        public string PartId { get; }
        public bool HasCondition { get; }
        public int ConditionCur { get; private set; }
        public int ConditionMax { get; private set; }
        public BodyPartKind Kind { get; }

        public IReadOnlyList<BodyPartNode> Children => _children;
        public IReadOnlyList<BodyPartEffect> Effects => _effects;

        public BodyPartNode(
            string partId,
            bool hasCondition,
            int conditionMax = 0,
            BodyPartKind kind = BodyPartKind.Organic)
        {
            PartId = partId;
            HasCondition = hasCondition;
            Kind = kind;
            if (hasCondition)
            {
                ConditionMax = conditionMax;
                ConditionCur = conditionMax;
            }
        }

        public void SetCondition(int current, int max)
        {
            if (!HasCondition)
                return;

            ConditionMax = max < 0 ? 0 : max;
            ConditionCur = current < 0
                ? 0
                : (current > ConditionMax ? ConditionMax : current);
        }

        /// <summary>팩토리·<see cref="ICharacterBody.TryAttach"/> 전용. 런타임 복원은 TryAttach만.</summary>
        internal void AddChild(BodyPartNode child)
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

        /// <summary>같은 effectId가 있으면 intensity를 max로. 없으면 추가. 변경 시 true.</summary>
        public bool EnsureEffectMinIntensity(
            string effectId,
            int intensity,
            float remainingSeconds = -1f)
        {
            if (string.IsNullOrEmpty(effectId) || intensity < 1)
                return false;

            for (int i = 0; i < _effects.Count; i++)
            {
                BodyPartEffect e = _effects[i];
                if (e.EffectId != effectId)
                    continue;
                if (e.Intensity >= intensity)
                    return false;
                _effects[i] = new BodyPartEffect(effectId, intensity, e.RemainingSeconds);
                return true;
            }

            _effects.Add(new BodyPartEffect(effectId, intensity, remainingSeconds));
            return true;
        }

        /// <summary>같은 effectId intensity를 reduceBy만큼 줄임. 0 이하면 제거. 변경 시 true.</summary>
        public bool ReduceEffectIntensity(string effectId, int reduceBy)
        {
            if (string.IsNullOrEmpty(effectId) || reduceBy <= 0)
                return false;

            for (int i = 0; i < _effects.Count; i++)
            {
                BodyPartEffect e = _effects[i];
                if (e.EffectId != effectId)
                    continue;

                int next = e.Intensity - reduceBy;
                if (next < 1)
                {
                    _effects.RemoveAt(i);
                    return true;
                }

                _effects[i] = new BodyPartEffect(effectId, next, e.RemainingSeconds);
                return true;
            }

            return false;
        }

        /// <summary>유한 효과 초를 줄이고 만료를 제거. 변경 시 true.</summary>
        public bool TickEffectDurations(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || _effects.Count == 0)
                return false;

            bool changed = false;
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                BodyPartEffect effect = _effects[i];
                if (effect.IsPermanent)
                    continue;

                float remaining = effect.RemainingSeconds - deltaSeconds;
                if (remaining <= 0f)
                {
                    _effects.RemoveAt(i);
                    changed = true;
                    continue;
                }

                _effects[i] = new BodyPartEffect(
                    effect.EffectId,
                    effect.Intensity,
                    remaining);
                changed = true;
            }

            return changed;
        }
    }
}
