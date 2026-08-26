// ============================================================

// BodyIllness — 출혈·감염·독소 틱 상수 SSOT

// ============================================================

// LLM 인덱스: docs/body/TUNING.md (숫자는 이 파일만)



namespace Garunnir.Runtime.Gameplay.Data

{

    /// <summary>밸런스 수치는 스캐폴딩. 공식은 BodyCapacity / BodyEffectTicker.</summary>

    public static class BodyIllness

    {

        #region Bleed



        /// <summary>베임·절단 소켓 등 상처 Bleed intensity 1당 초당 Blood01 감소. 소비: BodyEffectTicker.TickBleedBlood.</summary>

        public const float BleedBloodPerIntensityPerSecond = 0.001f;


        /// <summary>장기 파괴 organ_bleed intensity 1당 초당 Blood01 감소. 상처 Bleed와 별도.</summary>

        public const float OrganBleedBloodPerIntensityPerSecond = 0.004f;



        /// <summary>베임이 1 HP라도 남은 부위의 파생 Bleed intensity. 소켓·장기 파괴 Bleed와 별개.</summary>

        public const int CutBleedMinIntensity = 1;



        /// <summary>남은 베임 HP → 그 부위 파생 Bleed. 0이면 파생 없음(소켓·장기 Bleed는 유지).</summary>

        public static int BleedIntensityForCut(int cutHp) =>

            cutHp <= 0 ? 0 : CutBleedMinIntensity;



        #endregion



        #region Infection onset



        /// <summary>같은 부위 Bleed 지속 후 Infected 부여. 소비: BodyEffectTicker.TickInfectionOnsetNode.</summary>

        public const float InfectedOnsetSeconds = 20f;



        /// <summary>프로토타입 손 Bleed 초 — onset보다 짧게.</summary>

        public const float PrototypeBleedSeconds = 12f;



        #endregion



        #region Injury tend



        /// <summary>가슴 풀피에 해당하는 타박 tend 창. 1 HP당 = 이 값 / BaseCondition.</summary>

        public const float BruiseHealSeconds = 90f;



        /// <summary>유기 부위 잃은 HP 1당 회복 초. 가슴 풀피 = <see cref="BruiseHealSeconds"/>. 타박 tend.</summary>

        public const float InjuryHealSecondsPerHp = BruiseHealSeconds / CharacterBody.BaseCondition;



        public const float CutTendBruiseMul = 2f;

        public const float GunshotTendBruiseMul = 2f;

        public const float FractureTendBruiseMul = 4f;



        public const float CutTendSecondsPerHp = InjuryHealSecondsPerHp * CutTendBruiseMul;

        public const float GunshotTendSecondsPerHp = InjuryHealSecondsPerHp * GunshotTendBruiseMul;

        public const float FractureTendSecondsPerHp = InjuryHealSecondsPerHp * FractureTendBruiseMul;



        #endregion



        #region Infection race



        /// <summary>전신 감염 진행 속도. 소비: BodyEffectTicker.TickInfectionRace.</summary>

        public const float InfectedProgressPerSecond = 0.001f;



        /// <summary>전신 면역 진행 속도(× 여과 × 항생제). 소비: BodyEffectTicker.TickInfectionRace.</summary>

        public const float ImmunityPerSecond = 0.0012f;



        public const float InfectionConsciousnessK = 1f;



        #endregion



        #region Toxin



        public const float ToxinConsciousnessK = 1f;

        public const float ToxinFiltrationK = 1f;



        /// <summary>소비: BodyEffectTicker.TickToxinClear.</summary>

        public const float ToxinClearPerSecond = 0.02f;



        public const float RotToxinAdd = 0.15f;



        public const float MedToxinClear = 0.25f;

        public const int MedBleedIntensityReduce = 1;



        public const float ToxinMoodMin = 0.2f;

        public const float LowImmunityFiltration = 0.4f;



        #endregion



        #region Antibiotic



        public const string ItemAntibiotics = "antibiotics";

        public const string ItemWeakAntibiotic = "weak_antibiotic";

        public const string ItemStrongAntibiotic = "strong_antibiotic";

        public const string UseActionAntibiotic = "antibiotic";



        public const int AntibioticIntensityWeak = 1;

        public const int AntibioticIntensityRegular = 2;

        public const int AntibioticIntensityStrong = 3;



        public const float MedImmunityGainMulWeak = 1.5f;

        public const float MedImmunityGainMulRegular = 2f;

        public const float MedImmunityGainMulStrong = 3f;



        /// <summary>BN 1회 12시간. 기본 시계(실시간 1초 = 월드 1분)에서 World delta 초.</summary>

        public const float MedImmunityDurationSeconds = 12f * 60f;



        public static bool TryAntibioticIntensity(string itemId, out int intensity)

        {

            intensity = 0;

            if (string.IsNullOrEmpty(itemId))

                return false;

            if (string.Equals(itemId, ItemWeakAntibiotic, System.StringComparison.OrdinalIgnoreCase))

            {

                intensity = AntibioticIntensityWeak;

                return true;

            }



            if (string.Equals(itemId, ItemAntibiotics, System.StringComparison.OrdinalIgnoreCase) ||

                string.Equals(itemId, UseActionAntibiotic, System.StringComparison.OrdinalIgnoreCase))

            {

                intensity = AntibioticIntensityRegular;

                return true;

            }



            if (string.Equals(itemId, ItemStrongAntibiotic, System.StringComparison.OrdinalIgnoreCase))

            {

                intensity = AntibioticIntensityStrong;

                return true;

            }



            return false;

        }



        public static float ImmunityGainMul(int antibioticIntensity)

        {

            if (antibioticIntensity >= AntibioticIntensityStrong)

                return MedImmunityGainMulStrong;

            if (antibioticIntensity >= AntibioticIntensityRegular)

                return MedImmunityGainMulRegular;

            if (antibioticIntensity >= AntibioticIntensityWeak)

                return MedImmunityGainMulWeak;

            return 1f;

        }



        #endregion



        #region Bandage



        /// <summary>레거시 BN int_dur_factor. Dist 붕대는 영구(수동 벗기).</summary>

        public const float BandageSecondsPerIntensity = 6f * 60f;



        /// <summary>레거시 BN max_duration. Dist는 사용하지 않음.</summary>

        public const float BandageMaxDurationSeconds = 4f * 24f * 60f;



        /// <summary>BN effect_bandaged max_intensity.</summary>

        public const int BandageMaxIntensity = 16;



        /// <summary>감은 부위 tend 배율. BN healing_rate 기저 2. dirty와 무관.</summary>

        public const float BandageTendMul = 2f;



        /// <summary>막은 Blood01 흡수량 → bandage_dirty intensity.</summary>

        public const int BandageDirtyMax = 100;



        /// <summary>흡수 Blood01 이 값마다 dirty intensity +1. 소비: BodyEffectTicker.AbsorbIntoBandage.</summary>

        public const float BandageDirtyBloodPerPoint = 0.005f;



        /// <summary>깨끗한 붕대: Infected onset age 적립 배율 (&lt; 1). 소비: BodyEffectTicker.InfectedOnsetMul.</summary>

        public const float BandageCleanInfectedOnsetMul = 0.35f;



        /// <summary>dirty max일 때 Infected onset age 적립 배율 (&gt; 1).</summary>

        public const float BandageDirtyInfectedOnsetMul = 4f;



        #endregion



        #region Sever · organ bleed



        // 절단 후 남는 소켓(상완/대퇴는 가슴) Bleed intensity

        public const int SeverStumpBleedFinger = 2;

        public const int SeverStumpBleedHandFoot = 3;

        public const int SeverStumpBleedMidLimb = 4;

        public const int SeverStumpBleedRootLimb = 5;



        // 장기 HP0 파괴 출혈 intensity (뇌 제외)

        public const int OrganDestroyedBleedHeart = 8;

        public const int OrganDestroyedBleedLiver = 6;

        public const int OrganDestroyedBleedLung = 5;

        public const int OrganDestroyedBleedKidney = 4;

        public const int OrganDestroyedBleedStomach = 4;

        public const int OrganDestroyedBleedDefault = 3;



        #endregion

    }

}


