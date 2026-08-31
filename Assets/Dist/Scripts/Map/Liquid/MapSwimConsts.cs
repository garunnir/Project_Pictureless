// ============================================================
// MapSwimConsts — Wade/Swim/Dive·산소·탱크 이동 상수 SSOT
// ============================================================

namespace IsoTilemap
{
    /// <summary>숫자 진실원. 인덱스: docs/body/TUNING.md · 계약: docs/locomotion/SWIM.md</summary>
    public static class MapSwimConsts
    {
        /// <summary>Wade 진입 Fill01. 수중창 <see cref="MapFishConsts.UnderwaterShooterFill01"/>과 동일.</summary>
        public const float WadeFill01 = MapLiquidConsts.ShallowSeedFraction;

        /// <summary>Swim 가능 최소 컬럼 ml (가득 찬 1셀).</summary>
        public const int SwimColumnMl = MapLiquidConsts.DefaultMaxVolumeMl;

        public const float WadeSpeedFactor = 0.7f;
        public const float WadeSprintFactor = 0.85f;
        public const float SwimSpeedFactor = 0.55f;
        public const float DiveSpeedFactor = 0.45f;

        /// <summary>수직 상승·하강·응급 상승 공용 (m/s).</summary>
        public const float DiveVerticalSpeed = 2.2f;

        /// <summary>머리 셀 Fill01 이상이면 자동 Dive(머리 잠김).</summary>
        public const float HeadSubmergeFill01 = WadeFill01;

        /// <summary>발→머리 오프셋 (월드 m). 셀 크기와 무관한 대략치.</summary>
        public const float HeadHeightWorld = 1.6f;

        /// <summary>체내 O2 버퍼 (World 초, LungEff 곱).</summary>
        public const float BaseBreathHoldSeconds = 30f;

        /// <summary>머리 공기권 회복 rate (× LungEff).</summary>
        public const float BloodOxygenRecoverPerSecond = 0.35f;

        /// <summary>활성 탱크가 수중에서 차지를 소모하는 간격 (World 초).</summary>
        public const float DiveTankChargeIntervalSeconds = 60f;

        public const int DiveTankChargePerInterval = 1;

        /// <summary>탱크 charge 1개당 합산 O2 풀 초.</summary>
        public const float DiveTankSecondsPerCharge = DiveTankChargeIntervalSeconds;

        public const string DiveTankItemId = "dive_tank";
        public const string DiveTankUseActionType = "DIVE_TANK";

        public const int DiveTankMaxCharges = 60;
        public const int DiveTankInitialCharges = 60;

        /// <summary>액체 immersion 습윤 gain/초 (날씨 gain과 max).</summary>
        public const float LiquidWetnessGainWade = 0.04f;
        public const float LiquidWetnessGainSwim = 0.08f;
        public const float LiquidWetnessGainDive = 0.12f;
    }
}
