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
            {
                Collider collider = colliders[i];
                if (role == TileCollisionRole.WalkableOnly)
                {
                    collider.enabled = true;
                    continue;
                }

                // LogicalOnly: 솔리드는 끄고(이동 물리 비간섭), 트리거는 유지(포인터 피킹).
                collider.enabled = collider.isTrigger;
            }
        }
    }
}
