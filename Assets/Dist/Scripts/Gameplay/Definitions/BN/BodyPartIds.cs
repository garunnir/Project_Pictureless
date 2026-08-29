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

        // ── Vital organs (tree only — not Main/Severable/Thermal) ─
        public const string Brain = "brain";
        public const string Heart = "heart";
        public const string LungL = "lung_l";
        public const string LungR = "lung_r";
        public const string Liver = "liver";
        public const string Stomach = "stomach";
        public const string KidneyL = "kidney_l";
        public const string KidneyR = "kidney_r";

        public static readonly string[] MainConditionParts =
        {
            Head, Neck, Chest, Belly, Pelvis,
            UpperArmL, LowerArmL, HandL, FingerThumbL, FingerIndexL,
            UpperArmR, LowerArmR, HandR, FingerThumbR, FingerIndexR,
            ThighL, CalfL, FootL,
            ThighR, CalfR, FootR
        };

        public static readonly string[] FingerParts =
        {
            FingerThumbL, FingerIndexL, FingerThumbR, FingerIndexR
        };

        /// <summary>장기 노드. 조준·절단·체온 목록 밖.</summary>
        public static readonly string[] VitalOrgans =
        {
            Brain, Heart, LungL, LungR, Liver, Stomach, KidneyL, KidneyR
        };

        /// <summary>상태창 부위 행 = Main + VitalOrgans.</summary>
        public static readonly string[] StatusConditionParts = BuildStatusConditionParts();

        static string[] BuildStatusConditionParts()
        {
            var parts = new string[MainConditionParts.Length + VitalOrgans.Length];
            for (int i = 0; i < MainConditionParts.Length; i++)
                parts[i] = MainConditionParts[i];
            for (int i = 0; i < VitalOrgans.Length; i++)
                parts[MainConditionParts.Length + i] = VitalOrgans[i];
            return parts;
        }

        /// <summary>체온 틱·표시용 10부위 SSOT.</summary>
        public static readonly string[] ThermalParts =
        {
            Head, Chest,
            UpperArmL, UpperArmR,
            HandL, HandR,
            ThighL, ThighR,
            FootL, FootR
        };

        /// <summary>FrostbiteOnsetTempC 이하 지속 시 frostbite가 붙는 말단.</summary>
        public static readonly string[] FrostbiteParts =
        {
            Head, HandL, HandR, FootL, FootR
        };

        /// <summary>
        /// 복원·사지 UI용. 파괴 가능 여부와 무관 — 모든 메인 부위는 오버킬로 RemovePart 가능.
        /// </summary>
        public static readonly string[] SeverableParts =
        {
            UpperArmL, LowerArmL, HandL, FingerThumbL, FingerIndexL,
            UpperArmR, LowerArmR, HandR, FingerThumbR, FingerIndexR,
            ThighL, CalfL, FootL,
            ThighR, CalfR, FootR
        };

        static readonly Dictionary<string, string> MainConditionOf = new()
        {
            [Head] = Head,
            [Eyes] = Head,
            [Mouth] = Head,
            [Brain] = Brain,
            [Neck] = Neck,
            [Chest] = Chest,
            [Belly] = Belly,
            [Pelvis] = Pelvis,
            [Torso] = Chest,
            [Heart] = Heart,
            [LungL] = LungL,
            [LungR] = LungR,
            [Liver] = Liver,
            [Stomach] = Stomach,
            [KidneyL] = KidneyL,
            [KidneyR] = KidneyR,
            [UpperArmL] = UpperArmL,
            [LowerArmL] = LowerArmL,
            [HandL] = HandL,
            [FingerThumbL] = FingerThumbL,
            [FingerIndexL] = FingerIndexL,
            [ArmL] = UpperArmL,
            [UpperArmR] = UpperArmR,
            [LowerArmR] = LowerArmR,
            [HandR] = HandR,
            [FingerThumbR] = FingerThumbR,
            [FingerIndexR] = FingerIndexR,
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

        static readonly Dictionary<string, string> OrganParentOf = new()
        {
            [Brain] = Head,
            [Heart] = Chest,
            [LungL] = Chest,
            [LungR] = Chest,
            [Liver] = Belly,
            [Stomach] = Belly,
            [KidneyL] = Belly,
            [KidneyR] = Belly
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
            [HandL] = new[] { LowerArmL, FingerThumbL, FingerIndexL },
            [FingerThumbL] = new[] { HandL },
            [FingerIndexL] = new[] { HandL },
            [UpperArmR] = new[] { Chest, LowerArmR },
            [LowerArmR] = new[] { UpperArmR, HandR },
            [HandR] = new[] { LowerArmR, FingerThumbR, FingerIndexR },
            [FingerThumbR] = new[] { HandR },
            [FingerIndexR] = new[] { HandR },
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
            [Brain] = Head,
            [Neck] = Torso,
            [Chest] = Torso,
            [Belly] = Torso,
            [Pelvis] = Torso,
            [Torso] = Torso,
            [Heart] = Torso,
            [LungL] = Torso,
            [LungR] = Torso,
            [Liver] = Torso,
            [Stomach] = Torso,
            [KidneyL] = Torso,
            [KidneyR] = Torso,
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

        public static bool IsFinger(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return false;

            string id = ResolveNodeId(partId);
            for (int i = 0; i < FingerParts.Length; i++)
            {
                if (FingerParts[i] == id)
                    return true;
            }

            return false;
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

        public static bool IsVitalOrgan(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return false;

            string id = ResolveNodeId(partId);
            for (int i = 0; i < VitalOrgans.Length; i++)
            {
                if (VitalOrgans[i] == id)
                    return true;
            }

            return false;
        }

        /// <summary>장기 트리 부모 (head/chest/belly). 아니면 null.</summary>
        public static string GetOrganParentId(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return null;

            return OrganParentOf.TryGetValue(ResolveNodeId(partId), out string parent)
                ? parent
                : null;
        }

        /// <summary>
        /// 절단 후 남는 소켓 부모. 상완/대퇴·머리/가슴 루트는 null — stump는 호출측.
        /// 목→머리, 장기→OrganParent.
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
            if (id == FingerThumbL || id == FingerIndexL) return HandL;
            if (id == FingerThumbR || id == FingerIndexR) return HandR;
            if (id == Neck) return Head;
            string organParent = GetOrganParentId(id);
            if (!string.IsNullOrEmpty(organParent))
                return organParent;
            return null;
        }

        /// <summary>사지 루트(상완/대퇴) — stump Bleed를 chest에 둔다.</summary>
        public static bool IsLimbRoot(string partId)
        {
            string id = ResolveNodeId(partId);
            return id == UpperArmL || id == UpperArmR || id == ThighL || id == ThighR;
        }
    }
}
