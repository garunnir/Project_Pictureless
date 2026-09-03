// ============================================================
// IsoDepthSortKey — 이소 투명 정렬 비교 키 (y → x+z → x)
// ============================================================

using System;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// drawable 투명 렌더러 집합 정렬 키. <see cref="IsoVisibleDepthSortRegistry"/>가 연속 sortOrder를 부여한다.
    /// </summary>
    public readonly struct IsoDepthSortKey : IComparable<IsoDepthSortKey>
    {
        public int Y { get; }
        public int XZSum { get; }
        public int X { get; }

        public IsoDepthSortKey(int y, int xzSum, int x)
        {
            Y = y;
            XZSum = xzSum;
            X = x;
        }

        public static IsoDepthSortKey FromGridCell(Vector3Int gridCell) =>
            new(gridCell.y, gridCell.x + gridCell.z, gridCell.x);

        public static IsoDepthSortKey FromLiquidChunkCorner(Vector2Int chunk, int chunkSize, int minCellY)
        {
            int size = Mathf.Max(1, chunkSize);
            return FromGridCell(new Vector3Int(chunk.x * size, minCellY, chunk.y * size));
        }

        /// <summary>타일 슬롯별 정렬 셀 — 바닥은 CellAbove, 벽은 남동 쪽 인접 셀.</summary>
        public static IsoDepthSortKey FromTileView(TileView view)
        {
            if (view == null)
                return default;

            Vector3Int cell = view.gridPos;

            switch (view.placementSlot)
            {
                case TilePlacementSlot.HorizontalFace:
                    cell = new FloorFaceKey(view.gridPos, FloorFace.PosY).CellAbove;
                    break;

                case TilePlacementSlot.VerticalFace:
                {
                    var edge = new WallEdgeKey(view.gridPos, (WallFace)Mathf.Clamp(view.wallFace, 0, 1));
                    Vector3Int neighbor = edge.NeighborCell();
                    int sumAnchor = edge.Anchor.x + edge.Anchor.z;
                    int sumNeighbor = neighbor.x + neighbor.z;
                    cell = sumNeighbor >= sumAnchor ? neighbor : edge.Anchor;
                    cell.y = edge.Anchor.y;
                    break;
                }
            }

            return FromGridCell(cell);
        }

        public int CompareTo(IsoDepthSortKey other)
        {
            int c = Y.CompareTo(other.Y);
            if (c != 0)
                return c;

            c = XZSum.CompareTo(other.XZSum);
            if (c != 0)
                return c;

            return X.CompareTo(other.X);
        }
    }
}
