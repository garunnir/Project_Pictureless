// ============================================================
// Map presentation driver contracts — Dist.Map ↔ DistScript 경계
// ============================================================

using System;
using UnityEngine;

namespace IsoTilemap
{
    public interface IProximityBlendDriver
    {
        void Init(
            TileMapCacheHub hub,
            TileViewPresentationApplier applier,
            PlayerFloorVisibilityPolicy policy,
            Func<Camera> resolveCamera);

        void Shutdown();
    }

    public interface IFloorVisibilityDriver
    {
        void Init(PlayerFloorVisibilityPolicy policy, IFloorVisibilitySync visibilitySync);
        void Shutdown();
        void ApplyNow();
    }
}
