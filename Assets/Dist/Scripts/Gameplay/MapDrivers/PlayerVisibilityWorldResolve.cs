// ============================================================
// PlayerVisibilityWorldResolve — 가시성·오클루전 evaluate SSOT
// ============================================================
using IsoTilemap;
using UnityEngine;

/// <summary>
/// 타일 가시성·오클루전 드라이버 공용 evaluate.
/// <see cref="CharacterState.ResolveVisibilityWorldPoint"/> → <see cref="PlayerFloorVisibilityPolicy.ResolvePlayerOccupiedCell"/>.
/// 조준·비조준 동일 파이프라인(입력 월드만 다름). room·야외는 <paramref name="evaluationCell"/> + <see cref="TileMapCacheHub.GetVisitedForCell"/>.
/// </summary>
public static class PlayerVisibilityWorldResolve
{
    public static Vector3 ResolveVisibilityWorld(CharacterState state, float bodyHeightOffsetWorld = 0f)
    {
        if (state == null)
            return Vector3.zero;

        Vector3 world = state.ResolveVisibilityWorldPoint();
        if (!state.IsAiming)
            world.y += bodyHeightOffsetWorld;

        return world;
    }

    public static void ResolveEvaluation(
        CharacterState state,
        PlayerFloorVisibilityPolicy policy,
        float bodyHeightOffsetWorld,
        out Vector3 visibilityWorld,
        out Vector3Int evaluationCell,
        out Vector3Int footprint)
    {
        visibilityWorld = ResolveVisibilityWorld(state, bodyHeightOffsetWorld);
        footprint = state != null ? state.GridFootprint : CharacterGridFootprintDefaults.Default;

        if (state == null)
        {
            evaluationCell = Vector3Int.zero;
            return;
        }

        evaluationCell = policy != null
            ? policy.ResolvePlayerOccupiedCell(visibilityWorld.y, visibilityWorld)
            : state.GridPos;
    }
}
