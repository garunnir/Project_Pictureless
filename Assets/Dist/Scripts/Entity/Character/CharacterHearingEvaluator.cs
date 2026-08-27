// ============================================================
// CharacterHearingEvaluator — 3D 구형 청각 audibility (시야 API 미사용)
// ============================================================

using IsoTilemap;
using UnityEngine;

public static class CharacterHearingEvaluator
{
    public static bool CanDetect(
        Vector3 listenerFeet,
        Vector3 targetFeet,
        CharacterMotor targetMotor,
        float hearingRadius,
        IMapTopologyQuery query) =>
        TryEvaluateAudibility(
            listenerFeet,
            targetFeet,
            targetMotor,
            hearingRadius,
            query,
            out _);

    public static bool TryEvaluateAudibility(
        Vector3 listenerFeet,
        Vector3 targetFeet,
        CharacterMotor targetMotor,
        float hearingRadius,
        IMapTopologyQuery query,
        out float audibility01)
    {
        audibility01 = 0f;
        float radius = Mathf.Max(0f, hearingRadius);
        if (!CharacterHearingDefaults.IsWithinSphere(listenerFeet, targetFeet, radius))
            return false;

        if (targetMotor == null ||
            targetMotor.CurrentSpeed < CharacterHearingDefaults.MovementSpeedThreshold)
            return false;

        float dist = Vector3.Distance(listenerFeet, targetFeet);
        float distanceFactor = radius > 0f ? 1f - dist / radius : 0f;
        float occlusion = ComputeOcclusionProduct(listenerFeet, targetFeet, query);
        audibility01 = Mathf.Clamp01(distanceFactor * occlusion);
        return audibility01 >= CharacterHearingDefaults.DetectAudibilityThreshold;
    }

    static float ComputeOcclusionProduct(
        Vector3 listenerFeet,
        Vector3 targetFeet,
        IMapTopologyQuery query)
    {
        if (query == null)
            return 1f;

        float cellSize = query.CellSize;
        Vector3Int listenerCell = TileHelper.ConvertWorldToGrid(listenerFeet, cellSize);
        Vector3Int targetCell = TileHelper.ConvertWorldToGrid(targetFeet, cellSize);

        float product = 1f;
        int floorDelta = Mathf.Abs(targetCell.y - listenerCell.y);
        if (floorDelta > 0)
        {
            product *= Mathf.Pow(
                CharacterHearingDefaults.FloorAttenuationPerLevel,
                floorDelta);
        }

        int wallHits = CountWallHitsAlongSegment(
            query,
            listenerCell.x,
            listenerCell.z,
            targetCell.x,
            targetCell.z,
            listenerCell.y);
        if (wallHits > 0)
        {
            product *= Mathf.Pow(CharacterHearingDefaults.WallAttenuation, wallHits);
        }

        return Mathf.Clamp01(product);
    }

    static int CountWallHitsAlongSegment(
        IMapTopologyQuery query,
        int x0,
        int z0,
        int x1,
        int z1,
        int gridY)
    {
        if (x0 == x1 && z0 == z1)
            return 0;

        int dx = Mathf.Abs(x1 - x0);
        int dz = Mathf.Abs(z1 - z0);
        int sx = x0 < x1 ? 1 : -1;
        int sz = z0 < z1 ? 1 : -1;
        int err = dx - dz;

        int x = x0;
        int z = z0;
        var prev = new Vector3Int(x, gridY, z);
        int hits = 0;

        while (x != x1 || z != z1)
        {
            int stepFromX = prev.x;
            int stepFromZ = prev.z;

            int e2 = 2 * err;
            if (e2 > -dz)
            {
                err -= dz;
                x += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                z += sz;
            }

            var stepTo = new Vector3Int(x, gridY, z);

            if (stepTo.x != stepFromX && stepTo.z != stepFromZ)
            {
                var mid = new Vector3Int(stepTo.x, gridY, stepFromZ);
                if (SegmentStepBlocks(query, prev, mid))
                    hits++;
                if (SegmentStepBlocks(query, mid, stepTo))
                    hits++;
            }
            else if (stepTo.x != stepFromX || stepTo.z != stepFromZ)
            {
                if (SegmentStepBlocks(query, prev, stepTo))
                    hits++;
            }

            prev = stepTo;
        }

        return hits;
    }

    static bool SegmentStepBlocks(IMapTopologyQuery query, Vector3Int from, Vector3Int to) =>
        MapTopologyGridSegment.CrossesBlockingBetween(query, from, to);
}
