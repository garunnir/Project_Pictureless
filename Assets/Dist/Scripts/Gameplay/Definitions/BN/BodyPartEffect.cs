// ============================================================
// BodyPartEffect — 부위에 부착된 상태 효과 엔트리 (스캐폴딩)
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    public readonly struct BodyPartEffect
    {
        public readonly string EffectId;
        public readonly int Intensity;
        public readonly int RemainingTurns;

        public BodyPartEffect(string effectId, int intensity = 1, int remainingTurns = -1)
        {
            EffectId = effectId ?? string.Empty;
            Intensity = intensity;
            RemainingTurns = remainingTurns;
        }
    }

    /// <summary>프로토타입용 효과 ID SSOT. 카탈로그/틱은 후속.</summary>
    public static class BodyPartEffectIds
    {
        public const string Bleed = "bleed";
        public const string Fracture = "fracture";
        public const string Infected = "infected";
        public const string Regenerating = "regenerating";
        public const string Adrenaline = "adrenaline";
    }
}
