// ============================================================
// CharacterBody — 인간 anatomy 소유권 트리 런타임 (플레이어·NPC 공용)
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public sealed class CharacterBody : ICharacterBody
    {
        public const int BaseCondition = 60;
        public const int ConditionPerStr = 3;

        readonly List<BodyPartNode> _roots = new();

        public event Action Changed;

        public IReadOnlyList<BodyPartNode> Roots => _roots;

        public bool IsDeadState
        {
            get
            {
                if (!TryGet(BodyPartIds.Head, out BodyPartNode head) ||
                    head.ConditionCur <= 0)
                    return true;
                if (!TryGet(BodyPartIds.Chest, out BodyPartNode chest) ||
                    chest.ConditionCur <= 0)
                    return true;
                return false;
            }
        }

        public static CharacterBody CreateHumanDefault(int strength, bool prototypeSeed = true)
        {
            int conditionMax = BaseCondition + strength * ConditionPerStr;
            var body = new CharacterBody();

            BodyPartNode head = new(BodyPartIds.Head, true, conditionMax);
            head.AddChild(new BodyPartNode(BodyPartIds.Eyes, false));
            head.AddChild(new BodyPartNode(BodyPartIds.Mouth, false));
            head.AddChild(new BodyPartNode(BodyPartIds.Neck, true, conditionMax));
            body._roots.Add(head);

            BodyPartNode chest = new(BodyPartIds.Chest, true, conditionMax);
            chest.AddChild(new BodyPartNode(BodyPartIds.Belly, true, conditionMax));
            chest.AddChild(new BodyPartNode(BodyPartIds.Pelvis, true, conditionMax));
            body._roots.Add(chest);

            body._roots.Add(CreateArm(
                BodyPartIds.UpperArmL,
                BodyPartIds.LowerArmL,
                BodyPartIds.HandL,
                BodyPartIds.FingerThumbL,
                BodyPartIds.FingerIndexL,
                conditionMax));
            body._roots.Add(CreateArm(
                BodyPartIds.UpperArmR,
                BodyPartIds.LowerArmR,
                BodyPartIds.HandR,
                BodyPartIds.FingerThumbR,
                BodyPartIds.FingerIndexR,
                conditionMax));
            body._roots.Add(CreateLeg(
                BodyPartIds.ThighL,
                BodyPartIds.CalfL,
                BodyPartIds.FootL,
                conditionMax));
            body._roots.Add(CreateLeg(
                BodyPartIds.ThighR,
                BodyPartIds.CalfR,
                BodyPartIds.FootR,
                conditionMax));

            if (!prototypeSeed)
                return body;

            if (body.TryGet(BodyPartIds.HandL, out BodyPartNode seededHand))
            {
                seededHand.AddEffect(new BodyPartEffect(BodyPartEffectIds.Bleed, 1, 12f));
                seededHand.AddEffect(new BodyPartEffect(BodyPartEffectIds.Infected, 1, -1f));
            }

            if (body.TryGet(BodyPartIds.FingerIndexL, out BodyPartNode seededFinger))
                seededFinger.AddEffect(new BodyPartEffect(BodyPartEffectIds.Fracture, 1, -1f));

            return body;
        }

        public bool TryGet(string partId, out BodyPartNode node)
        {
            node = null;
            if (string.IsNullOrEmpty(partId))
                return false;

            string resolved = BodyPartIds.ResolveNodeId(partId);
            for (int i = 0; i < _roots.Count; i++)
            {
                if (TryFind(_roots[i], resolved, out node))
                    return true;
            }

            return false;
        }

        public bool Has(string partId) => TryGet(partId, out _);

        public bool RemovePart(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return false;

            string resolved = BodyPartIds.ResolveNodeId(partId);
            for (int i = 0; i < _roots.Count; i++)
            {
                if (_roots[i].PartId != resolved)
                    continue;

                _roots.RemoveAt(i);
                Changed?.Invoke();
                return true;
            }

            for (int i = 0; i < _roots.Count; i++)
            {
                if (!TryRemoveUnder(_roots[i], resolved))
                    continue;

                Changed?.Invoke();
                return true;
            }

            return false;
        }

        public bool TryAttach(string parentId, BodyPartNode node)
        {
            if (!TryAttachCore(parentId, node))
                return false;

            Changed?.Invoke();
            return true;
        }

        public CharacterBodyDto ToDto()
        {
            var parts = new List<CharacterBodyPartDto>();
            for (int i = 0; i < _roots.Count; i++)
                AppendPartDto(_roots[i], string.Empty, parts);

            return new CharacterBodyDto { parts = parts.ToArray() };
        }

        public static CharacterBody CreateFromDto(CharacterBodyDto dto)
        {
            var body = new CharacterBody();
            body.ReplaceFromDto(dto);
            return body;
        }

        public void FromDto(CharacterBodyDto dto)
        {
            ReplaceFromDto(dto);
            Changed?.Invoke();
        }

        void ReplaceFromDto(CharacterBodyDto dto)
        {
            _roots.Clear();
            CharacterBodyPartDto[] parts = dto != null ? dto.parts : null;
            if (parts == null || parts.Length == 0)
                return;

            bool[] done = new bool[parts.Length];
            int remaining = parts.Length;
            int guard = parts.Length + 1;
            while (remaining > 0 && guard-- > 0)
            {
                int attachedThisPass = 0;
                for (int i = 0; i < parts.Length; i++)
                {
                    if (done[i])
                        continue;

                    CharacterBodyPartDto part = parts[i];
                    if (part == null || string.IsNullOrEmpty(part.partId))
                    {
                        done[i] = true;
                        remaining--;
                        continue;
                    }

                    if (!string.IsNullOrEmpty(part.parent) && !Has(part.parent))
                        continue;

                    if (!TryAttachCore(part.parent, CreateNodeFromDto(part)))
                    {
                        done[i] = true;
                        remaining--;
                        continue;
                    }

                    done[i] = true;
                    remaining--;
                    attachedThisPass++;
                }

                if (attachedThisPass == 0)
                    break;
            }
        }

        bool TryAttachCore(string parentId, BodyPartNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.PartId) || Has(node.PartId))
                return false;

            if (string.IsNullOrEmpty(parentId))
            {
                _roots.Add(node);
                return true;
            }

            if (!TryGet(parentId, out BodyPartNode parent))
                return false;

            parent.AddChild(node);
            return true;
        }

        static BodyPartNode CreateNodeFromDto(CharacterBodyPartDto part)
        {
            int max = part.hasCondition ? part.conditionMax : 0;
            var node = new BodyPartNode(part.partId, part.hasCondition, max, part.kind);
            if (part.hasCondition)
                node.SetCondition(part.conditionCur, part.conditionMax);

            BodyPartEffectDto[] effects = part.effects;
            if (effects == null)
                return node;

            for (int i = 0; i < effects.Length; i++)
            {
                BodyPartEffectDto e = effects[i];
                if (e == null || string.IsNullOrEmpty(e.effectId))
                    continue;

                node.AddEffect(new BodyPartEffect(e.effectId, e.intensity, e.remainingSeconds));
            }

            return node;
        }

        static void AppendPartDto(BodyPartNode node, string parentId, List<CharacterBodyPartDto> into)
        {
            IReadOnlyList<BodyPartEffect> effects = node.Effects;
            var effectDtos = new BodyPartEffectDto[effects.Count];
            for (int i = 0; i < effects.Count; i++)
            {
                BodyPartEffect e = effects[i];
                effectDtos[i] = new BodyPartEffectDto
                {
                    effectId = e.EffectId,
                    intensity = e.Intensity,
                    remainingSeconds = e.RemainingSeconds
                };
            }

            into.Add(new CharacterBodyPartDto
            {
                partId = node.PartId,
                parent = parentId ?? string.Empty,
                kind = node.Kind,
                hasCondition = node.HasCondition,
                conditionCur = node.ConditionCur,
                conditionMax = node.ConditionMax,
                effects = effectDtos
            });

            IReadOnlyList<BodyPartNode> children = node.Children;
            for (int i = 0; i < children.Count; i++)
                AppendPartDto(children[i], node.PartId, into);
        }

        public int GetConditionCur(string mainConditionPartId)
        {
            if (!TryGet(mainConditionPartId, out BodyPartNode node) ||
                !node.HasCondition)
                return 0;
            return node.ConditionCur;
        }

        public int GetConditionMax(string mainConditionPartId)
        {
            if (!TryGet(mainConditionPartId, out BodyPartNode node) ||
                !node.HasCondition)
                return 0;
            return node.ConditionMax;
        }

        public void SetCondition(string mainConditionPartId, int current, int max)
        {
            if (!TryGet(mainConditionPartId, out BodyPartNode node) ||
                !node.HasCondition)
                return;

            node.SetCondition(current, max);
            Changed?.Invoke();
        }

        public bool AddEffect(string partId, BodyPartEffect effect)
        {
            if (!TryGet(partId, out BodyPartNode node))
                return false;

            node.AddEffect(effect);
            Changed?.Invoke();
            return true;
        }

        public bool ClearEffectsOn(string partId)
        {
            if (!TryGet(partId, out BodyPartNode node))
                return false;

            if (node.Effects.Count == 0)
                return false;

            node.ClearEffects();
            Changed?.Invoke();
            return true;
        }

        public bool TickEffectDurations(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
                return false;

            bool changed = false;
            for (int i = 0; i < _roots.Count; i++)
            {
                if (TickNode(_roots[i], deltaSeconds))
                    changed = true;
            }

            if (changed)
                Changed?.Invoke();
            return changed;
        }

        public void CollectEffectsUnder(string partId, List<BodyPartEffect> into, bool includeDescendants)
        {
            if (into == null || !TryGet(partId, out BodyPartNode root))
                return;

            AppendEffects(root, into, includeDescendants);
        }

        static bool TickNode(BodyPartNode node, float deltaSeconds)
        {
            bool changed = node.TickEffectDurations(deltaSeconds);
            IReadOnlyList<BodyPartNode> children = node.Children;
            for (int i = 0; i < children.Count; i++)
            {
                if (TickNode(children[i], deltaSeconds))
                    changed = true;
            }

            return changed;
        }

        static void AppendEffects(BodyPartNode node, List<BodyPartEffect> into, bool includeDescendants)
        {
            IReadOnlyList<BodyPartEffect> effects = node.Effects;
            for (int i = 0; i < effects.Count; i++)
                into.Add(effects[i]);

            if (!includeDescendants)
                return;

            IReadOnlyList<BodyPartNode> children = node.Children;
            for (int i = 0; i < children.Count; i++)
                AppendEffects(children[i], into, true);
        }

        static bool TryFind(BodyPartNode node, string partId, out BodyPartNode found)
        {
            if (node.PartId == partId)
            {
                found = node;
                return true;
            }

            IReadOnlyList<BodyPartNode> children = node.Children;
            for (int i = 0; i < children.Count; i++)
            {
                if (TryFind(children[i], partId, out found))
                    return true;
            }

            found = null;
            return false;
        }

        static bool TryRemoveUnder(BodyPartNode parent, string partId)
        {
            if (parent.RemoveDirectChild(partId))
                return true;

            IReadOnlyList<BodyPartNode> children = parent.Children;
            for (int i = 0; i < children.Count; i++)
            {
                if (TryRemoveUnder(children[i], partId))
                    return true;
            }

            return false;
        }

        public static bool TryCreateLimbFrom(
            string startPartId,
            int conditionMax,
            BodyPartKind kind,
            out BodyPartNode subtree)
        {
            subtree = null;
            if (string.IsNullOrEmpty(startPartId))
                return false;

            string id = BodyPartIds.ResolveNodeId(startPartId);
            if (id == BodyPartIds.UpperArmL)
            {
                subtree = CreateArm(
                    BodyPartIds.UpperArmL,
                    BodyPartIds.LowerArmL,
                    BodyPartIds.HandL,
                    BodyPartIds.FingerThumbL,
                    BodyPartIds.FingerIndexL,
                    conditionMax,
                    kind);
                return true;
            }

            if (id == BodyPartIds.UpperArmR)
            {
                subtree = CreateArm(
                    BodyPartIds.UpperArmR,
                    BodyPartIds.LowerArmR,
                    BodyPartIds.HandR,
                    BodyPartIds.FingerThumbR,
                    BodyPartIds.FingerIndexR,
                    conditionMax,
                    kind);
                return true;
            }

            if (id == BodyPartIds.LowerArmL)
            {
                subtree = CreateArmFromLower(
                    BodyPartIds.LowerArmL,
                    BodyPartIds.HandL,
                    BodyPartIds.FingerThumbL,
                    BodyPartIds.FingerIndexL,
                    conditionMax,
                    kind);
                return true;
            }

            if (id == BodyPartIds.LowerArmR)
            {
                subtree = CreateArmFromLower(
                    BodyPartIds.LowerArmR,
                    BodyPartIds.HandR,
                    BodyPartIds.FingerThumbR,
                    BodyPartIds.FingerIndexR,
                    conditionMax,
                    kind);
                return true;
            }

            if (id == BodyPartIds.HandL)
            {
                subtree = CreateHand(
                    BodyPartIds.HandL,
                    BodyPartIds.FingerThumbL,
                    BodyPartIds.FingerIndexL,
                    conditionMax,
                    kind);
                return true;
            }

            if (id == BodyPartIds.HandR)
            {
                subtree = CreateHand(
                    BodyPartIds.HandR,
                    BodyPartIds.FingerThumbR,
                    BodyPartIds.FingerIndexR,
                    conditionMax,
                    kind);
                return true;
            }

            if (id == BodyPartIds.ThighL)
            {
                subtree = CreateLeg(
                    BodyPartIds.ThighL,
                    BodyPartIds.CalfL,
                    BodyPartIds.FootL,
                    conditionMax,
                    kind);
                return true;
            }

            if (id == BodyPartIds.ThighR)
            {
                subtree = CreateLeg(
                    BodyPartIds.ThighR,
                    BodyPartIds.CalfR,
                    BodyPartIds.FootR,
                    conditionMax,
                    kind);
                return true;
            }

            if (id == BodyPartIds.CalfL)
            {
                subtree = CreateLegFromCalf(
                    BodyPartIds.CalfL,
                    BodyPartIds.FootL,
                    conditionMax,
                    kind);
                return true;
            }

            if (id == BodyPartIds.CalfR)
            {
                subtree = CreateLegFromCalf(
                    BodyPartIds.CalfR,
                    BodyPartIds.FootR,
                    conditionMax,
                    kind);
                return true;
            }

            if (id == BodyPartIds.FootL)
            {
                subtree = new BodyPartNode(BodyPartIds.FootL, true, conditionMax, kind);
                return true;
            }

            if (id == BodyPartIds.FootR)
            {
                subtree = new BodyPartNode(BodyPartIds.FootR, true, conditionMax, kind);
                return true;
            }

            return false;
        }

        static BodyPartNode CreateArm(
            string upperId,
            string lowerId,
            string handId,
            string thumbId,
            string indexId,
            int conditionMax,
            BodyPartKind kind = BodyPartKind.Organic)
        {
            BodyPartNode upper = new(upperId, true, conditionMax, kind);
            upper.AddChild(CreateArmFromLower(lowerId, handId, thumbId, indexId, conditionMax, kind));
            return upper;
        }

        static BodyPartNode CreateArmFromLower(
            string lowerId,
            string handId,
            string thumbId,
            string indexId,
            int conditionMax,
            BodyPartKind kind)
        {
            BodyPartNode lower = new(lowerId, true, conditionMax, kind);
            lower.AddChild(CreateHand(handId, thumbId, indexId, conditionMax, kind));
            return lower;
        }

        static BodyPartNode CreateHand(
            string handId,
            string thumbId,
            string indexId,
            int conditionMax,
            BodyPartKind kind)
        {
            BodyPartNode hand = new(handId, true, conditionMax, kind);
            hand.AddChild(new BodyPartNode(thumbId, false, 0, kind));
            hand.AddChild(new BodyPartNode(indexId, false, 0, kind));
            return hand;
        }

        static BodyPartNode CreateLeg(
            string thighId,
            string calfId,
            string footId,
            int conditionMax,
            BodyPartKind kind = BodyPartKind.Organic)
        {
            BodyPartNode thigh = new(thighId, true, conditionMax, kind);
            thigh.AddChild(CreateLegFromCalf(calfId, footId, conditionMax, kind));
            return thigh;
        }

        static BodyPartNode CreateLegFromCalf(
            string calfId,
            string footId,
            int conditionMax,
            BodyPartKind kind)
        {
            BodyPartNode calf = new(calfId, true, conditionMax, kind);
            calf.AddChild(new BodyPartNode(footId, true, conditionMax, kind));
            return calf;
        }
    }
}
