// ============================================================
// BodyPartEffect — 부위에 부착된 상태 효과 엔트리
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    public readonly struct BodyPartEffect
    {
        public readonly string EffectId;
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
        public const string Fracture = "fracture";
        public const string Infected = "infected";
        public const string Regenerating = "regenerating";
        public const string Adrenaline = "adrenaline";
        public const string Frostbite = "frostbite";
        public const string Heat = "heat";
        public const string Bloated = "bloated";
        public const string Toxin = "toxin";
    }
}
