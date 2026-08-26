// ============================================================
// CharacterSightFadeEvaluator — 공통 시야 부채꼴 + 눈높이 3D LOS(수직 Floor) → 페이드
// ============================================================

using IsoTilemap;
using UnityEngine;

public static class CharacterSightFadeEvaluator
{
    const float MinDirSqr = 1e-8f;
    const float CellEpsilonMeters = 1e-3f;

    /// <param name="forwardXZ">시야 전방 — PlayerSight/Spot과 동일 XZ.</param>
    /// <param name="spotAngleDegrees">시야 전체 각 (Definition / CharacterVision).</param>
    public static float EvaluateTarget(
        Vector3 playerFeetWorld,
        Vector3 targetFeetWorld,
        Vector3 forwardXZ,
        float effectiveDetectRadius,
        float spotAngleDegrees,
        float innerSpotAngleDegrees,
        in CharacterSightFadeSettings settings,
        MapTopologyLineCast lineCast)
    {
        float outer = Mathf.Max(0f, effectiveDetectRadius);
        float dx = targetFeetWorld.x - playerFeetWorld.x;
        float dz = targetFeetWorld.z - playerFeetWorld.z;
        float distanceXZ = Mathf.Sqrt(dx * dx + dz * dz);

        if (!CharacterVisionDefaults.IsWithinConeXZ(
                playerFeetWorld,
                forwardXZ,
                targetFeetWorld,
                outer,
                spotAngleDegrees))
            return 0f;

        float fadeWidth = Mathf.Max(0f, settings.FadeWidthMeters);
        float inner = Mathf.Max(0f, outer - fadeWidth);
        float distanceFade;
        if (outer <= 0f)
            distanceFade = 0f;
        else if (outer <= inner)
            distanceFade = 1f;
        else
            distanceFade = Mathf.InverseLerp(outer, inner, distanceXZ);

        float angleFade = EvaluateAngleFade01(
            forwardXZ, dx, dz, distanceXZ, spotAngleDegrees, innerSpotAngleDegrees);
        if (angleFade <= 0f)
            return 0f;

        // 눈높이 3D LOS: 시선 높이 벽 + 발 층 벽 + 층 Floor 교차(수직)
        if (settings.LineOfSightEnabled && lineCast != null)
        {
            float eyeY = Mathf.Max(0f, settings.LosHeightOffsetMeters);
            Vector3 origin = playerFeetWorld + Vector3.up * eyeY;
            Vector3 destination = targetFeetWorld + Vector3.up * eyeY;
            float dist3d = Vector3.Distance(origin, destination);
            if (dist3d > MinDirSqr)
            {
                int feetGridY = TileHelper.ConvertWorldToGrid(playerFeetWorld, lineCast.CellSize).y;
                if (lineCast.TryGetBlockingDistance3D(origin, destination, feetGridY, out float hit) &&
                    hit < dist3d - CellEpsilonMeters)
                {
                    return 0f;
                }
            }
        }

        return Mathf.Clamp01(distanceFade * angleFade);
    }

    static float EvaluateAngleFade01(
        Vector3 forwardXZ,
        float dx,
        float dz,
        float distanceXZ,
        float spotAngleDegrees,
        float innerSpotAngleDegrees)
    {
        float halfOuter = Mathf.Max(0f, spotAngleDegrees) * 0.5f;
        if (halfOuter >= 179.9f)
            return 1f;

        float angleDeg = CharacterVisionDefaults.AngleDegreesFromForwardXZ(
            forwardXZ, dx, dz, distanceXZ);
        if (angleDeg < 0f)
            return 0f;
        if (angleDeg >= halfOuter)
            return 0f;

        float halfInner = Mathf.Clamp(innerSpotAngleDegrees * 0.5f, 0f, halfOuter);
        if (angleDeg <= halfInner || halfOuter <= halfInner)
            return 1f;

        return Mathf.InverseLerp(halfOuter, halfInner, angleDeg);
    }
}
