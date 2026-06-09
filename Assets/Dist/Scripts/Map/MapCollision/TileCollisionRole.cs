using UnityEngine;

namespace IsoTilemap
{
    public enum TileCollisionRole
    {
        LogicalOnly = 0,
        WalkableOnly = 1,
    }

    public static class TileCollisionPolicy
    {
        public static TileCollisionRole Resolve(byte collisionFlags) =>
            TileCollisionFlagsUtil.Has(collisionFlags, TileCollisionFlags.UsePhysicsCollider)
                ? TileCollisionRole.WalkableOnly
                : TileCollisionRole.LogicalOnly;

        public static void Apply(TileView view, byte collisionFlags)
        {
            if (view == null)
                return;

            var role = Resolve(collisionFlags);

            var colliders = view.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = role == TileCollisionRole.WalkableOnly;
        }
    }
}
