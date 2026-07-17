// ============================================================
// BodyPartIds — 신체 부위 ID / 메인 HP 매핑 SSOT (소유권 트리용)
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class BodyPartIds
    {
        // ── Main HP parts (UI rows) ───────────────────────────
        public const string Head = "head";
        public const string Torso = "torso";
        public const string ArmL = "arm_l";
        public const string ArmR = "arm_r";
        public const string LegL = "leg_l";
        public const string LegR = "leg_r";

        // ── Detail anatomy (data + hover only) ────────────────
        public const string Eyes = "eyes";
        public const string Mouth = "mouth";
        public const string HandL = "hand_l";
        public const string HandR = "hand_r";
        public const string FootL = "foot_l";
        public const string FootR = "foot_r";

        // Finger placeholders under hands (UI rows never expand for these)
        public const string FingerThumbL = "finger_thumb_l";
        public const string FingerIndexL = "finger_index_l";
        public const string FingerThumbR = "finger_thumb_r";
        public const string FingerIndexR = "finger_index_r";

        public static readonly string[] MainHpParts =
        {
            Head, Torso, ArmL, ArmR, LegL, LegR
        };

        static readonly Dictionary<string, string> MainHpOf = new()
        {
            [Head] = Head,
            [Eyes] = Head,
            [Mouth] = Head,
            [Torso] = Torso,
            [ArmL] = ArmL,
            [HandL] = ArmL,
            [FingerThumbL] = ArmL,
            [FingerIndexL] = ArmL,
            [ArmR] = ArmR,
            [HandR] = ArmR,
            [FingerThumbR] = ArmR,
            [FingerIndexR] = ArmR,
            [LegL] = LegL,
            [FootL] = LegL,
            [LegR] = LegR,
            [FootR] = LegR
        };

        public static bool IsMainHpPart(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return false;

            for (int i = 0; i < MainHpParts.Length; i++)
            {
                if (MainHpParts[i] == partId)
                    return true;
            }

            return false;
        }

        public static string GetMainHpPart(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return null;

            return MainHpOf.TryGetValue(partId, out string main) ? main : null;
        }
    }
}
