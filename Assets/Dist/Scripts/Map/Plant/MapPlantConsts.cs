// ============================================================
// MapPlantConsts — 맵 식물 오버레이 타겟팅·농사 플래그·단계 외형 SSOT
// ============================================================

using UnityEngine;

namespace IsoTilemap
{
    public static class MapPlantConsts
    {
        public const float OverlayScale = 0.35f;
        public const float OverlayYOffset = 0.18f;
        public const float OverlayHeight = 0.4f;

        public const string DigQualityId = "DIG";
        public const int MinDigQualityLevel = 1;
        public const string FertilizerFlag = "FERTILIZER";
        public const string GreenhouseFlag = "GREENHOUSE";

        /// <summary>
        /// Dist.Map clock bridge fallback. Matches <c>WorldClockSettings.DefaultMinutesPerDay</c>
        /// when DistScript has not bound <see cref="MapClockSnapshot.GetMinutesPerDay"/>.
        /// </summary>
        public const int FallbackMinutesPerDay = 24 * 60;

        public const string OverlayShaderUrpUnlit = "Universal Render Pipeline/Unlit";
        public const string OverlayShaderUnlitColor = "Unlit/Color";

        public static readonly Vector3 OverlayScaleSeed = new Vector3(0.16f, 0.12f, 0.16f);
        public static readonly Vector3 OverlayScaleSeedling = new Vector3(0.22f, 0.32f, 0.22f);
        public static readonly Vector3 OverlayScaleMature = new Vector3(0.28f, 0.52f, 0.28f);
        public static readonly Vector3 OverlayScaleHarvestable = new Vector3(0.34f, 0.68f, 0.34f);
        public static readonly Vector3 OverlayScaleWithered = new Vector3(0.26f, 0.18f, 0.26f);

        public static readonly Color OverlayColorSeed = new Color(0.45f, 0.32f, 0.12f, 1f);
        public static readonly Color OverlayColorSeedling = new Color(0.42f, 0.72f, 0.28f, 1f);
        public static readonly Color OverlayColorMature = new Color(0.22f, 0.55f, 0.18f, 1f);
        public static readonly Color OverlayColorHarvestable = new Color(0.72f, 0.78f, 0.18f, 1f);
        public static readonly Color OverlayColorWithered = new Color(0.38f, 0.30f, 0.22f, 1f);
    }
}
