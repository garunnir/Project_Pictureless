using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public interface IMapTopologyQuery
    {
        float CellSize { get; }

        bool TryGetCellTiles(int x, int z, int gridY, out IReadOnlyList<TileData> list);

        bool CellHasSolidWall(int x, int z, int gridY);

        /// <summary>점유 인덱스 — OccupiedCell·VerticalFace·HorizontalFace incident 포함.</summary>
        bool CellHasOccupancy(int x, int z, int gridY);

        bool CellHasFloor(int x, int z, int gridY);

        bool TryGetEdgeBetween(Vector3Int cellA, Vector3Int cellB, out TileData edgeWall);
    }
}
