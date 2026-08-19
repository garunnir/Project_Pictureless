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

        /// <summary>체온 틱·표시용 10부위 SSOT.</summary>
        public static readonly string[] ThermalParts =
        {
            Head, Chest,
            UpperArmL, UpperArmR,
            HandL, HandR,
            ThighL, ThighR,
            FootL, FootR
        };

        /// <summary>Cold 지속 시 frostbite가 붙는 말단.</summary>
        public static readonly string[] FrostbiteParts =
        {
            Head, HandL, HandR, FootL, FootR
        };

        /// <summary>HP 0일 때 RemovePart 대상. 팔/다리 체인만 (head/neck/chest/belly/pelvis 제외).</summary>
        public static readonly string[] SeverableParts =
        {
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

        static readonly Dictionary<string, string> ThermalPartOf = new()
        {
            [Head] = Head,
            [Eyes] = Head,
            [Mouth] = Head,
            [Neck] = Chest,
            [Chest] = Chest,
            [Belly] = Chest,
            [Pelvis] = Chest,
            [Torso] = Chest,
            [UpperArmL] = UpperArmL,
            [LowerArmL] = UpperArmL,
            [ArmL] = UpperArmL,
            [HandL] = HandL,
            [FingerThumbL] = HandL,
            [FingerIndexL] = HandL,
            [UpperArmR] = UpperArmR,
            [LowerArmR] = UpperArmR,
            [ArmR] = UpperArmR,
            [HandR] = HandR,
            [FingerThumbR] = HandR,
            [FingerIndexR] = HandR,
            [ThighL] = ThighL,
            [CalfL] = ThighL,
            [LegL] = ThighL,
            [FootL] = FootL,
            [ThighR] = ThighR,
            [CalfR] = ThighR,
            [LegR] = ThighR,
            [FootR] = FootR
        };

        static readonly Dictionary<string, string[]> AdjacentMains = new()
        {
            [Head] = new[] { Neck },
            [Neck] = new[] { Head, Chest },
            [Chest] = new[] { Neck, Belly, UpperArmL, UpperArmR },
            [Belly] = new[] { Chest, Pelvis },
            [Pelvis] = new[] { Belly, ThighL, ThighR },
            [UpperArmL] = new[] { Chest, LowerArmL },
            [LowerArmL] = new[] { UpperArmL, HandL },
            [HandL] = new[] { LowerArmL },
            [UpperArmR] = new[] { Chest, LowerArmR },
            [LowerArmR] = new[] { UpperArmR, HandR },
            [HandR] = new[] { LowerArmR },
            [ThighL] = new[] { Pelvis, CalfL },
            [CalfL] = new[] { ThighL, FootL },
            [FootL] = new[] { CalfL },
            [ThighR] = new[] { Pelvis, CalfR },
            [CalfR] = new[] { ThighR, FootR },
            [FootR] = new[] { CalfR }
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

        public static bool IsThermalPart(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return false;

            for (int i = 0; i < ThermalParts.Length; i++)
            {
                if (ThermalParts[i] == partId)
                    return true;
            }

            return false;
        }

        public static int IndexOfThermalPart(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return -1;

            for (int i = 0; i < ThermalParts.Length; i++)
            {
                if (ThermalParts[i] == partId)
                    return i;
            }

            return -1;
        }

        public static string GetThermalPart(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return null;

            return ThermalPartOf.TryGetValue(partId, out string thermal) ? thermal : null;
        }

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

        /// <summary>해부 인접 메인 부위. dest에 쓰고 개수 반환. 할당 없음.</summary>
        public static int WriteAdjacentMains(string partId, string[] dest)
        {
            if (dest == null || dest.Length == 0 || string.IsNullOrEmpty(partId))
                return 0;

            string id = GetMainConditionPart(partId) ?? ResolveNodeId(partId);
            if (!AdjacentMains.TryGetValue(id, out string[] adj) || adj == null)
                return 0;

            int n = 0;
            for (int i = 0; i < adj.Length && n < dest.Length; i++)
                dest[n++] = adj[i];
            return n;
        }

        public static bool IsSeverable(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return false;

            string id = ResolveNodeId(partId);
            for (int i = 0; i < SeverableParts.Length; i++)
            {
                if (SeverableParts[i] == id)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 절단 후 남는 소켓 부모. 상완/대퇴는 루트(null) — TryAttach(null)이 루트로 채운다.
        /// </summary>
        public static string GetSocketParentId(string partId)
        {
            string id = ResolveNodeId(partId);
            if (id == LowerArmL) return UpperArmL;
            if (id == HandL) return LowerArmL;
            if (id == LowerArmR) return UpperArmR;
            if (id == HandR) return LowerArmR;
            if (id == CalfL) return ThighL;
            if (id == FootL) return CalfL;
            if (id == CalfR) return ThighR;
            if (id == FootR) return CalfR;
            return null;
        }
    }
}
