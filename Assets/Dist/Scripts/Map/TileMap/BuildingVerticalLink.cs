// ============================================================
// BuildingVerticalLink — 동일 (x,z) 열 + 그리드 Y+1 구조물로 상향 연결 판정
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public static class BuildingVerticalLink
    {
        /// <summary>그리드 <paramref name="gridY"/> 바로 위(<c>y+1</c>) 셀에 구조 타일이 있는지 봅니다.</summary>
        public static bool CellHasStructuralAbove(
            IMapModelReadOnly map,
            int x,
            int z,
            int gridY) =>
            CellHasStructural(map, x, z, gridY + 1);

        public static bool CellHasStructural(
            IMapModelReadOnly map,
            int x,
            int z,
            int gridY)
        {
            using var tiles = new CellTileQueryBuffer(map, x, z, gridY);
            var list = tiles.Tiles;
            for (int i = 0; i < list.Count; i++)
            {
                if (IsStructuralType((TileView.TileType)list[i].identity.tileType))
                    return true;
            }

            return false;
        }

        public static bool CellHasVerticalSource(
            IMapModelReadOnly map,
            int x,
            int z,
            int gridY,
            int buildingId)
        {
            using var tiles = new CellTileQueryBuffer(map, x, z, gridY);
            var list = tiles.Tiles;
            for (int i = 0; i < list.Count; i++)
            {
                var tile = list[i];
                var type = (TileView.TileType)tile.identity.tileType;
                if (!IsVerticalSourceType(type))
                    continue;

                if (tile.identity.buildingId == buildingId)
                    return true;
            }

            return false;
        }

        static bool IsStructuralType(TileView.TileType type) =>
            type is TileView.TileType.Floor
                or TileView.TileType.Wall
                or TileView.TileType.EdgeWall;

        static bool IsVerticalSourceType(TileView.TileType type) =>
            type is TileView.TileType.Floor
                or TileView.TileType.Wall
                or TileView.TileType.EdgeWall;

        /// <summary>셀 타일 + 인시던트 EdgeWall(멀티 Y 인덱스 포함)을 함께 조회합니다.</summary>
        sealed class CellTileQueryBuffer : System.IDisposable
        {
            readonly List<TileData> _scratch = new();
            public IReadOnlyList<TileData> Tiles => _scratch;

            public CellTileQueryBuffer(IMapModelReadOnly map, int x, int z, int gridY)
            {
                if (map.TryGetCellTiles(x, z, gridY, out var baseList))
                    _scratch.AddRange(baseList);

                map.EdgeBinder.AppendIncidentEdges(new Vector3Int(x, gridY, z), _scratch);
            }

            public void Dispose()
            {
            }
        }
    }
}

