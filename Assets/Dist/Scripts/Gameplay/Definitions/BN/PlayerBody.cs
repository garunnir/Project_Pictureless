// ============================================================
// PlayerBody — 인간 anatomy 소유권 트리 런타임
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public sealed class PlayerBody : IPlayerBody
    {
        public const int BaseHp = 60;
        public const int HpPerStr = 3;

        readonly List<BodyPartNode> _roots = new();

        public event Action Changed;

        public IReadOnlyList<BodyPartNode> Roots => _roots;

        public bool IsDeadState
        {
            get
            {
                if (!TryGet(BodyPartIds.Head, out BodyPartNode head) || head.HpCur <= 0)
                    return true;
                if (!TryGet(BodyPartIds.Torso, out BodyPartNode torso) || torso.HpCur <= 0)
                    return true;
                return false;
            }
        }

        public static PlayerBody CreateHumanDefault(int strength)
        {
            int hpMax = BaseHp + strength * HpPerStr;
            var body = new PlayerBody();

            BodyPartNode head = new(BodyPartIds.Head, true, hpMax);
            head.AddChild(new BodyPartNode(BodyPartIds.Eyes, false));
            head.AddChild(new BodyPartNode(BodyPartIds.Mouth, false));
            body._roots.Add(head);

            body._roots.Add(new BodyPartNode(BodyPartIds.Torso, true, hpMax));

            BodyPartNode armL = new(BodyPartIds.ArmL, true, hpMax);
            BodyPartNode handL = new(BodyPartIds.HandL, false);
            handL.AddChild(new BodyPartNode(BodyPartIds.FingerThumbL, false));
            handL.AddChild(new BodyPartNode(BodyPartIds.FingerIndexL, false));
            armL.AddChild(handL);
            body._roots.Add(armL);

            BodyPartNode armR = new(BodyPartIds.ArmR, true, hpMax);
            BodyPartNode handR = new(BodyPartIds.HandR, false);
            handR.AddChild(new BodyPartNode(BodyPartIds.FingerThumbR, false));
            handR.AddChild(new BodyPartNode(BodyPartIds.FingerIndexR, false));
            armR.AddChild(handR);
            body._roots.Add(armR);

            BodyPartNode legL = new(BodyPartIds.LegL, true, hpMax);
            legL.AddChild(new BodyPartNode(BodyPartIds.FootL, false));
            body._roots.Add(legL);

            BodyPartNode legR = new(BodyPartIds.LegR, true, hpMax);
            legR.AddChild(new BodyPartNode(BodyPartIds.FootR, false));
            body._roots.Add(legR);

            // Prototype seed: show something in the hover detail panel.
            if (body.TryGet(BodyPartIds.HandL, out BodyPartNode seededHand))
            {
                seededHand.AddEffect(new BodyPartEffect(BodyPartEffectIds.Bleed, 1, 12));
                seededHand.AddEffect(new BodyPartEffect(BodyPartEffectIds.Infected, 1, -1));
            }

            if (body.TryGet(BodyPartIds.FingerIndexL, out BodyPartNode seededFinger))
                seededFinger.AddEffect(new BodyPartEffect(BodyPartEffectIds.Fracture, 1, -1));

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

        public int GetHpCur(string mainHpPartId)
        {
            if (!TryGet(mainHpPartId, out BodyPartNode node) || !node.HoldsHp)
                return 0;
            return node.HpCur;
        }

        public int GetHpMax(string mainHpPartId)
        {
            if (!TryGet(mainHpPartId, out BodyPartNode node) || !node.HoldsHp)
                return 0;
            return node.HpMax;
        }

        public void SetHp(string mainHpPartId, int current, int max)
        {
            if (!TryGet(mainHpPartId, out BodyPartNode node) || !node.HoldsHp)
                return;

            node.SetHp(current, max);
            Changed?.Invoke();
        }

        public void CollectEffectsUnder(string partId, List<BodyPartEffect> into, bool includeDescendants)
        {
            if (into == null || !TryGet(partId, out BodyPartNode root))
                return;

            AppendEffects(root, into, includeDescendants);
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
