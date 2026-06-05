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
        public static TileCollisionRole Resolve(TileView.TileType type, string prefabId)
        {
            if (type is TileView.TileType.Floor or TileView.TileType.Wall or TileView.TileType.EdgeWall)
                return TileCollisionRole.LogicalOnly;

            if (!string.IsNullOrEmpty(prefabId) &&
                prefabId.StartsWith("Furniture/Box", System.StringComparison.Ordinal))
                return TileCollisionRole.WalkableOnly;

            return TileCollisionRole.WalkableOnly;
        }

        public static void Apply(TileView view)
        {
            if (view == null)
                return;

            var role = Resolve(view.tileType, view.prefabId);

            var colliders = view.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = role == TileCollisionRole.WalkableOnly;
        }
    }
}
