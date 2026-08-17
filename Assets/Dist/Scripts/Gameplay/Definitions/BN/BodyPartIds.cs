// ============================================================
// BodyPartIds — 신체 부위 ID / 메인 컨디션 매핑 SSOT (소유권 트리용)
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class BodyPartIds
    {
        // ── Fine condition parts (chibi / combat) ─────────────
        public const string Head = "head";
        public const string Neck = "neck";
        public const string Chest = "chest";
        public const string Belly = "belly";
        public const string Pelvis = "pelvis";
        public const string UpperArmL = "upper_arm_l";
        public const string LowerArmL = "lower_arm_l";
        public const string UpperArmR = "upper_arm_r";
        public const string LowerArmR = "lower_arm_r";
        public const string ThighL = "thigh_l";
        public const string CalfL = "calf_l";
        public const string ThighR = "thigh_r";
        public const string CalfR = "calf_r";

        // ── Legacy coverage / aim aliases (not tree nodes) ────
        public const string Torso = "torso";
        public const string ArmL = "arm_l";
        public const string ArmR = "arm_r";
        public const string LegL = "leg_l";
        public const string LegR = "leg_r";

        // ── Detail anatomy ────────────────────────────────────
        public const string Eyes = "eyes";
        public const string Mouth = "mouth";
        public const string HandL = "hand_l";
        public const string HandR = "hand_r";
        public const string FootL = "foot_l";
        public const string FootR = "foot_r";

        public const string FingerThumbL = "finger_thumb_l";
        public const string FingerIndexL = "finger_index_l";
        public const string FingerThumbR = "finger_thumb_r";
        public const string FingerIndexR = "finger_index_r";

        public static readonly string[] MainConditionParts =
        {
            Head, Neck, Chest, Belly, Pelvis,
            UpperArmL, LowerArmL, HandL,
            UpperArmR, LowerArmR, HandR,
            ThighL, CalfL, FootL,
            ThighR, CalfR, FootR
        };

        static readonly Dictionary<string, string> MainConditionOf = new()
        {
            [Head] = Head,
            [Eyes] = Head,
            [Mouth] = Head,
            [Neck] = Neck,
            [Chest] = Chest,
            [Belly] = Belly,
            [Pelvis] = Pelvis,
            [Torso] = Chest,
            [UpperArmL] = UpperArmL,
            [LowerArmL] = LowerArmL,
            [HandL] = HandL,
            [FingerThumbL] = HandL,
            [FingerIndexL] = HandL,
            [ArmL] = UpperArmL,
            [UpperArmR] = UpperArmR,
            [LowerArmR] = LowerArmR,
            [HandR] = HandR,
            [FingerThumbR] = HandR,
            [FingerIndexR] = HandR,
            [ArmR] = UpperArmR,
            [ThighL] = ThighL,
            [CalfL] = CalfL,
            [FootL] = FootL,
            [LegL] = ThighL,
            [ThighR] = ThighR,
            [CalfR] = CalfR,
            [FootR] = FootR,
            [LegR] = ThighR
        };

        static readonly Dictionary<string, string> NodeAliasOf = new()
        {
            [Torso] = Chest,
            [ArmL] = UpperArmL,
            [ArmR] = UpperArmR,
            [LegL] = ThighL,
            [LegR] = ThighR
        };

        static readonly Dictionary<string, string> CoverGroupOf = new()
        {
            [Head] = Head,
            [Eyes] = Head,
            [Mouth] = Head,
            [Neck] = Torso,
            [Chest] = Torso,
            [Belly] = Torso,
            [Pelvis] = Torso,
            [Torso] = Torso,
            [UpperArmL] = ArmL,
            [LowerArmL] = ArmL,
            [HandL] = ArmL,
            [FingerThumbL] = ArmL,
            [FingerIndexL] = ArmL,
            [ArmL] = ArmL,
            [UpperArmR] = ArmR,
            [LowerArmR] = ArmR,
            [HandR] = ArmR,
            [FingerThumbR] = ArmR,
            [FingerIndexR] = ArmR,
            [ArmR] = ArmR,
            [ThighL] = LegL,
            [CalfL] = LegL,
            [FootL] = LegL,
            [LegL] = LegL,
            [ThighR] = LegR,
            [CalfR] = LegR,
            [FootR] = LegR,
            [LegR] = LegR
        };

        public static bool IsMainConditionPart(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return false;

            for (int i = 0; i < MainConditionParts.Length; i++)
            {
                if (MainConditionParts[i] == partId)
                    return true;
            }

            return false;
        }

        public static string GetMainConditionPart(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return null;

            return MainConditionOf.TryGetValue(partId, out string main) ? main : null;
        }

        public static string ResolveNodeId(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return partId;

            return NodeAliasOf.TryGetValue(partId, out string node) ? node : partId;
        }

        public static string GetCoverGroup(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return null;

            return CoverGroupOf.TryGetValue(partId, out string group) ? group : null;
        }
    }
}
