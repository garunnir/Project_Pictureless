// ============================================================
// BodyPartEffect — 부위에 부착된 상태 효과 엔트리
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    public readonly struct BodyPartEffect
    {
        public readonly string EffectId;
        /// <summary>
        /// 부상(bruise/cut/gunshot/fracture) = HP 점수. 0–1이 아님.
        /// 혈량·독·감염 진행은 ICharacterBody의 *01 필드.
        /// </summary>
        public readonly int Intensity;
        /// <summary>남은 초. -1 = 영구.</summary>
        public readonly float RemainingSeconds;

        public BodyPartEffect(string effectId, int intensity = 1, float remainingSeconds = -1f)
        {
            EffectId = effectId ?? string.Empty;
            Intensity = intensity;
            RemainingSeconds = remainingSeconds;
        }

        public bool IsPermanent => RemainingSeconds < 0f;
    }

    /// <summary>프로토타입용 효과 ID SSOT.</summary>
    public static class BodyPartEffectIds
    {
        public const string Bleed = "bleed";
        public const string Bruise = "bruise";
        public const string Cut = "cut";
        public const string Gunshot = "gunshot";
        public const string Fracture = "fracture";
        public const string Infected = "infected";
        public const string Regenerating = "regenerating";
        public const string Adrenaline = "adrenaline";
        public const string Frostbite = "frostbite";
        public const string Heat = "heat";
        public const string Bloated = "bloated";
        public const string Toxin = "toxin";
        public const string Antibiotic = "antibiotic";
        public const string Bandaged = "bandaged";
        /// <summary>붕대에 밴 피. intensity 0..<see cref="BodyIllness.BandageDirtyMax"/>.</summary>
        public const string BandageDirty = "bandage_dirty";
        /// <summary>지혈제. 파생 Bleed 재부착 금지(새 cut이면 해제).</summary>
        public const string Hemostatic = "hemostatic";
    }
}
