// ============================================================
// VaultConsts — 담넘기·벽넘기 상수 SSOT
// ============================================================

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
    }
}
