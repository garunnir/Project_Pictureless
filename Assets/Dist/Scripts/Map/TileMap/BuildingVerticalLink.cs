// ============================================================
// BuildingVerticalLink — 동일 (x,z) 열 + band+1 구조물로 상향 연결 판정
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public static class BuildingVerticalLink
    {
        public static bool CellHasStructural(
            IMapModelReadOnly map,
            int x,
            int z,
            int band,
            bool includeIncidentEdgeWalls = false)
        {
            using var tiles = new CellTileQueryBuffer(map, x, z, band, includeIncidentEdgeWalls);
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
            int band,
            int buildingId,
            bool includeIncidentEdgeWalls = false)
        {
            using var tiles = new CellTileQueryBuffer(map, x, z, band, includeIncidentEdgeWalls);
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
                or TileView.TileType.EdgeWall
                or TileView.TileType.Obstacle;

        static bool IsVerticalSourceType(TileView.TileType type) =>
            type is TileView.TileType.Floor
                or TileView.TileType.Wall
                or TileView.TileType.EdgeWall;

        /// <summary>
        /// 셀 타일 조회 기본값은 기존 동작(incident edge 제외)을 유지합니다.
        /// 필요 시 includeIncidentEdgeWalls=true로 인접 EdgeWall을 함께 조회합니다.
        /// </summary>
        sealed class CellTileQueryBuffer : System.IDisposable
        {
            readonly List<TileData> _scratch = new();
            public IReadOnlyList<TileData> Tiles => _scratch;

            public CellTileQueryBuffer(
                IMapModelReadOnly map,
                int x,
                int z,
                int band,
                bool includeIncidentEdgeWalls)
            {
                if (map.TryGetCellTiles(x, z, band, out var baseList))
                    _scratch.AddRange(baseList);

                if (!includeIncidentEdgeWalls)
                    return;

                map.EdgeBinder.AppendIncidentEdges(new Vector3Int(x, band, z), _scratch);
            }

            public void Dispose()
            {
            }
        }
    }
}
