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
                if (!TryGet(BodyPartIds.Torso, out BodyPartNode torso) ||
                    torso.ConditionCur <= 0)
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
            body._roots.Add(head);

            body._roots.Add(new BodyPartNode(BodyPartIds.Torso, true, conditionMax));

            BodyPartNode armL = new(BodyPartIds.ArmL, true, conditionMax);
            BodyPartNode handL = new(BodyPartIds.HandL, false);
            handL.AddChild(new BodyPartNode(BodyPartIds.FingerThumbL, false));
            handL.AddChild(new BodyPartNode(BodyPartIds.FingerIndexL, false));
            armL.AddChild(handL);
            body._roots.Add(armL);

            BodyPartNode armR = new(BodyPartIds.ArmR, true, conditionMax);
            BodyPartNode handR = new(BodyPartIds.HandR, false);
            handR.AddChild(new BodyPartNode(BodyPartIds.FingerThumbR, false));
            handR.AddChild(new BodyPartNode(BodyPartIds.FingerIndexR, false));
            armR.AddChild(handR);
            body._roots.Add(armR);

            BodyPartNode legL = new(BodyPartIds.LegL, true, conditionMax);
            legL.AddChild(new BodyPartNode(BodyPartIds.FootL, false));
            body._roots.Add(legL);

            BodyPartNode legR = new(BodyPartIds.LegR, true, conditionMax);
            legR.AddChild(new BodyPartNode(BodyPartIds.FootR, false));
            body._roots.Add(legR);

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

            for (int i = 0; i < _roots.Count; i++)
            {
                if (TryFind(_roots[i], partId, out node))
                    return true;
            }

            return false;
        }

        public bool Has(string partId) => TryGet(partId, out _);

        public bool RemovePart(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return false;

            for (int i = 0; i < _roots.Count; i++)
            {
                if (_roots[i].PartId != partId)
                    continue;

                _roots.RemoveAt(i);
                Changed?.Invoke();
                return true;
            }

            for (int i = 0; i < _roots.Count; i++)
            {
                if (!TryRemoveUnder(_roots[i], partId))
                    continue;

                Changed?.Invoke();
                return true;
            }

            return false;
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
    }
}
