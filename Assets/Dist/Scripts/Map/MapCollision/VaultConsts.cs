// ============================================================
// VaultConsts — 담넘기·벽넘기 상수 SSOT
// ============================================================

using UnityEngine;

namespace IsoTilemap
{
    /// <summary>숫자 진실원. 계약: docs/locomotion/VAULT.md</summary>
    public static class VaultConsts
    {
        /// <summary>E 홀드 vault 시전 임계 (Realtime이 아닌 Player 채널 초).</summary>
        public const float HoldSeconds = 0.35f;

        /// <summary>달리기 자동 vault 재시도 쿨다운 (Player 채널 초).</summary>
        public const float AutoRetryCooldown = 0.45f;

        /// <summary>달리기 자동 vault: 프로브 범위 안에서 MoveDir 전진 속도(m/s) 하한.</summary>
        public const float AutoSprintMinApproachSpeedMps = 2.5f;

        /// <summary>Mantle 프로브: 발밑에서 이동 방향으로 스캔하는 최대 앞 칸 수(1=바로 앞 열만).</summary>
        public const int MantleProbeMaxAheadCells = 1;

        /// <summary>접근 속도 duration 스케일: 이 속도(m/s) 이하면 배율 1.</summary>
        public const float DurationScaleWalkSpeedMps = 3f;

        /// <summary>접근 속도 duration 스케일: 이 속도(m/s) 이상이면 <see cref="DurationMinScale"/>.</summary>
        public const float DurationScaleSprintSpeedMps = 6f;

        /// <summary>최대 가속 시 vault duration 배율 하한 (빠를수록 짧게).</summary>
        public const float DurationMinScale = 0.65f;

        /// <summary>시전 순간 MoveDir 전진 속도(m/s)로 duration 배율을 계산한다.</summary>
        public static float ResolveDurationScale(float approachSpeedMps)
        {
            if (DurationScaleSprintSpeedMps <= DurationScaleWalkSpeedMps)
                return 1f;

            float t = Mathf.InverseLerp(
                DurationScaleWalkSpeedMps,
                DurationScaleSprintSpeedMps,
                approachSpeedMps);
            return Mathf.Lerp(1f, DurationMinScale, t);
        }

        /// <summary>낮은담 CrossOver 기본 모션 초 (클립 없으면).</summary>
        public const float LowCrossDurationSeconds = 0.45f;

        /// <summary>낮은담 Mantle 기본 모션 초.</summary>
        public const float LowMantleDurationSeconds = 0.55f;

        /// <summary>높은담 CrossOver 기본 모션 초.</summary>
        public const float HighCrossDurationSeconds = 0.7f;

        /// <summary>높은담 Mantle 기본 모션 초.</summary>
        public const float HighMantleDurationSeconds = 0.85f;

        /// <summary>CrossOver 정점 추가 높이 (셀 단위 비율).</summary>
        public const float CrossPeakHeightCells = 0.35f;

        /// <summary>Mantle 중간 키프레임 t (0..1).</summary>
        public const float MantleMidT = 0.45f;

        /// <summary>High Mantle: XZ 이동 시작 normalizedTime (xz 커브 없을 때 선형 폴백).</summary>
        public const float HighMantleXzStartT = 0.5f;

        /// <summary>Mantle IK: 손목 좌우 반폭 (셀 단위).</summary>
        public const float MantleIkHandHalfSpanCells = 0.25f;

        /// <summary>Mantle IK: 립 중심을 착지 셀 안쪽(접근 반대)으로 밀기 (셀 단위).</summary>
        public const float MantleIkLedgeInsetCells = 0.5f;

        /// <summary>Mantle IK: 립 높이 보정 (셀 단위).</summary>
        public const float MantleIkLedgeHeightOffsetCells = 0.05f;

        /// <summary>Mantle IK: 손바닥 회전 weight = position weight × 이 값.</summary>
        public const float MantleIkRotationWeightScale = 0.65f;

        /// <summary>Mantle IK weight ramp 시작 (Progress01).</summary>
        public const float MantleIkGrabStartT = 0.08f;

        /// <summary>Mantle IK weight peak (Progress01).</summary>
        public const float MantleIkGrabPeakT = 0.32f;

        /// <summary>Mantle IK weight ramp 종료 (Progress01).</summary>
        public const float MantleIkGrabEndT = 0.78f;

        /// <summary>Mantle Progress01 → 손 IK weight (0..1).</summary>
        public static float ResolveMantleIkWeight(float progress01)
        {
            progress01 = Mathf.Clamp01(progress01);
            if (progress01 < MantleIkGrabStartT || progress01 > MantleIkGrabEndT)
                return 0f;

            if (progress01 <= MantleIkGrabPeakT)
            {
                return Mathf.InverseLerp(
                    MantleIkGrabStartT,
                    MantleIkGrabPeakT,
                    progress01);
            }

            return Mathf.InverseLerp(
                MantleIkGrabEndT,
                MantleIkGrabPeakT,
                progress01);
        }
    }
}
