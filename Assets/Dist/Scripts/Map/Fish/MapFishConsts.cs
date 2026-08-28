// ============================================================
// MapFishConsts — 낚시 인접·품질 게이트 SSOT
// ============================================================

using UnityEngine;

namespace IsoTilemap
{
    public static class MapFishConsts
    {
        /// <summary>낚시 대상 물 셀 인접 판정 XZ Chebyshev 반경 (자기 셀 제외).</summary>
        public const int FishingAdjacentRangeCells = 1;

        public const string FishingQualityId = "FISHING";
        public const int MinFishingQualityLevel = 1;

        public const string FishPoorFlag = "FISH_POOR";
        public const string FishGoodFlag = "FISH_GOOD";

        public const string DefaultFishItemId = "fish";

        /// <summary>낚싯대 Cast 적용 가능 XZ Chebyshev 셀 반경 (목표 물 셀 포함).</summary>
        public const int CastActionRangeCells = 1;

        /// <summary>비-Cast 낚시 Arrive 월드 stopping = CellSize × 이 배율.</summary>
        public const float CellArriveStoppingCellFraction = 0.55f;

        /// <summary>낚시 Cast Work 대기초 (Catalog null 폴백).</summary>
        public const float CastWorkDurationSeconds = 3f;

        public const float FishPoorCatchMultiplier = 0.5f;
        public const float FishGoodCatchMultiplier = 2f;
        public const float FishPoorLootMultiplier = 0.5f;
        public const float FishGoodLootMultiplier = 2f;
        public const float FishingQualityLevelCatchBonus = 0.15f;

        /// <summary>
        /// 낚시·통발 가능 최소 컬럼 누적 ml (<see cref="MapLiquidQuery.ColumnMlDownward"/>).
        /// 셀 하나는 약 1,065,390 ml에서 클램프되므로 이 값은 수직 2셀 이상(분지)을 요구한다.
        /// </summary>
        public const int FishableColumnMl = 2_000_000;

        /// <summary>
        /// 수중창(S3) 발밑 셀 최소 충만도. 컬럼 수심이 아니라 국소 Fill01을 본다 —
        /// 얕은 물에서도 되던 구 SHALLOW_WATER 동작과 패리티를 맞추기 위해 시드 비율과 같은 값을 쓴다.
        /// </summary>
        public const float UnderwaterShooterFill01 = MapLiquidConsts.ShallowSeedFraction;

        /// <summary>수중창(S3) 육상 사거리 배율 — CombatHitscan 소비처용 SSOT.</summary>
        public const float UnderwaterGunLandRangeMultiplier = 0.1f;

        public const float TargetPreviewAlpha = 0.55f;
        public static readonly Color TargetPreviewValid = new Color(0.22f, 0.58f, 0.92f, TargetPreviewAlpha);
        public static readonly Color TargetPreviewInvalid = new Color(0.88f, 0.22f, 0.18f, TargetPreviewAlpha);

        public const string FishTrapItemId = "fish_trap";
        public const string FishBaitAmmoType = "fish_bait";

        /// <summary>통발 tick 간격(월드 분). Catch-up·런타임 tick SSOT.</summary>
        public const int TrapTickIntervalMinutes = 360;

        /// <summary>tick당 fish 1마리 적립 확률 (미끼 있을 때).</summary>
        public const float TrapCatchChancePerTick = 0.65f;

        public const float DeployTrapWorkDurationSeconds = 2f;
        public const float CollectTrapWorkDurationSeconds = 1.5f;

        public static readonly Color TrapOverlayColor = new Color(0.18f, 0.42f, 0.62f, 1f);
        public static readonly Vector3 TrapOverlayScale = new Vector3(0.42f, 0.22f, 0.42f);
    }
}
