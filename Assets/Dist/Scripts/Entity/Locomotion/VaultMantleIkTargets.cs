// ============================================================
// VaultMantleIkTargets — Mantle 벽 립 손 IK 타깃 (프로브 결과 → 월드)
// ============================================================

using IsoTilemap;
using UnityEngine;

public static class VaultMantleIkTargets
{
    public static bool TryGetHandTargets(
        in VaultCandidate candidate,
        float cellSize,
        out Vector3 leftHandWorld,
        out Vector3 rightHandWorld,
        out Quaternion handRotationWorld)
    {
        leftHandWorld = default;
        rightHandWorld = default;
        handRotationWorld = Quaternion.identity;

        if (candidate.Style != VaultCrossStyle.Mantle)
            return false;

        if (cellSize <= 0f)
            cellSize = 1f;

        Vector3 approach = candidate.ApproachDirXZ;
        approach.y = 0f;
        if (approach.sqrMagnitude < 1e-6f)
            return false;

        approach.Normalize();
        Vector3 lateral = Vector3.Cross(Vector3.up, approach).normalized;

        Vector3 ledgeCenter = TileHelper.ConvertGridToWorldPos(candidate.LandingFeetCell, cellSize);
        ledgeCenter -= approach * (VaultConsts.MantleIkLedgeInsetCells * cellSize);
        ledgeCenter.y += VaultConsts.MantleIkLedgeHeightOffsetCells * cellSize;

        float halfSpan = VaultConsts.MantleIkHandHalfSpanCells * cellSize;
        leftHandWorld = ledgeCenter - lateral * halfSpan;
        rightHandWorld = ledgeCenter + lateral * halfSpan;
        handRotationWorld = Quaternion.LookRotation(-approach, Vector3.up);
        return true;
    }
}
