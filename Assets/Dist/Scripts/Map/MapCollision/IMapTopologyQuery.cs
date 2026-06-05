using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public interface IMapTopologyQuery
    {
        float CellSize { get; }

        bool TryGetCellTiles(int x, int z, int band, out IReadOnlyList<TileData> list);

        bool CellHasSolidWall(int x, int z, int band);

        bool CellHasFloor(int x, int z, int band);

        bool TryGetEdgeBetween(Vector3Int cellA, Vector3Int cellB, out TileData edgeWall);
    }
}
