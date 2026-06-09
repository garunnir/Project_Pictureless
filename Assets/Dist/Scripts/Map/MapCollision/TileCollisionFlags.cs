// ============================================================
// TileCollisionFlags — TileDefinition에서 bake되는 충돌·오클루전 비트 플래그
// ============================================================
using System;
using System.Collections.Generic;

namespace IsoTilemap
{
    [Flags]
    public enum TileCollisionFlags : byte
    {
        None = 0,
        BlocksOccupiedCells = 1 << 0,
        ProvidesLogicalFloor = 1 << 1,
        UsePhysicsCollider = 1 << 2,
        BlocksEdge = 1 << 3,
        OccludesOccupiedCells = 1 << 4,
        OccludesEdge = 1 << 5,
        /// <summary>방 BFS(<see cref="FloorRoomFloodFill"/>) 경계. 통행(<see cref="BlocksEdge"/>)과 독립.</summary>
        SeparatesRoom = 1 << 6,
    }

    public static class TileCollisionFlagsUtil
    {
        public static bool Has(byte flags, TileCollisionFlags flag) =>
            ((TileCollisionFlags)flags & flag) != 0;

        public static bool CellBlocksOccupied(IReadOnlyList<TileData> list)
        {
            if (list == null)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (Has(list[i].identity.collisionFlags, TileCollisionFlags.BlocksOccupiedCells))
                    return true;
            }

            return false;
        }

        public static bool CellProvidesLogicalFloor(IReadOnlyList<TileData> list)
        {
            if (list == null)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (Has(list[i].identity.collisionFlags, TileCollisionFlags.ProvidesLogicalFloor))
                    return true;
            }

            return false;
        }

        public static bool EdgeBlocksPassage(in TileData edge) =>
            Has(edge.identity.collisionFlags, TileCollisionFlags.BlocksEdge);

        /// <summary>
        /// 방 BFS 경계. <see cref="TileCollisionFlags.SeparatesRoom"/> 또는 레거시 bake의 <see cref="TileCollisionFlags.BlocksEdge"/>.
        /// </summary>
        public static bool EdgeSeparatesRoom(in TileData edge)
        {
            byte flags = edge.identity.collisionFlags;
            return Has(flags, TileCollisionFlags.SeparatesRoom)
                || Has(flags, TileCollisionFlags.BlocksEdge);
        }

        public static bool TileOccludesOccupiedCells(in TileData tile) =>
            Has(tile.identity.collisionFlags, TileCollisionFlags.OccludesOccupiedCells);

        public static bool TileOccludesEdge(in TileData edge) =>
            Has(edge.identity.collisionFlags, TileCollisionFlags.OccludesEdge);
    }

    public static class TileCollisionProfile
    {
        /// <summary>레거시·에디터 프리셋. 런타임 bake는 <see cref="FromDefinitionForTileType"/> 사용.</summary>
        public static byte FromDefinition(TileDefinition def) =>
            FromDefinitionForTileType(0, def);

        /// <summary>
        /// EdgeWall은 <see cref="TileEdgeCollision"/>만, 그 외는 <see cref="TileOccupiedCellCollision"/>만 flatten.
        /// EdgeWall의 Physics Collider는 occupied.usePhysicsCollider만 추가로 반영합니다.
        /// </summary>
        public static byte FromDefinitionForTileType(byte tileType, TileDefinition def)
        {
            if (def == null)
                return 0;

            if (tileType == (byte)TileView.TileType.EdgeWall)
            {
                byte flags = FlattenEdge(def.edge);
                if (def.occupied.usePhysicsCollider)
                    flags |= (byte)TileCollisionFlags.UsePhysicsCollider;
                return flags;
            }

            return FlattenOccupied(def.occupied);
        }

        static byte FlattenOccupied(in TileOccupiedCellCollision occupied)
        {
            byte flags = 0;

            if (occupied.providesLogicalFloor)
                flags |= (byte)TileCollisionFlags.ProvidesLogicalFloor;
            if (occupied.usePhysicsCollider)
                flags |= (byte)TileCollisionFlags.UsePhysicsCollider;

            bool blocksOccupied = occupied.splitPassageAndOcclusion
                ? occupied.blocksOccupiedCells
                : occupied.blocksPassageAndOcclusion;
            bool occludesOccupied = occupied.splitPassageAndOcclusion
                ? occupied.occludesOccupiedCells
                : occupied.blocksPassageAndOcclusion;

            if (blocksOccupied)
                flags |= (byte)TileCollisionFlags.BlocksOccupiedCells;
            if (occludesOccupied)
                flags |= (byte)TileCollisionFlags.OccludesOccupiedCells;

            return flags;
        }

        static byte FlattenEdge(in TileEdgeCollision edge)
        {
            byte flags = 0;

            bool blocksEdgePassage = edge.splitPassageAndOcclusion
                ? edge.blocksEdge
                : edge.blocksPassageAndOcclusion;
            bool occludesEdgePassage = edge.splitPassageAndOcclusion
                ? edge.occludesEdge
                : edge.blocksPassageAndOcclusion;
            bool separatesRoom = edge.splitPassageAndOcclusion
                ? edge.separatesRoom
                : edge.blocksPassageAndOcclusion;

            if (blocksEdgePassage)
                flags |= (byte)TileCollisionFlags.BlocksEdge;
            if (occludesEdgePassage)
                flags |= (byte)TileCollisionFlags.OccludesEdge;
            if (separatesRoom)
                flags |= (byte)TileCollisionFlags.SeparatesRoom;

            return flags;
        }
    }
}
