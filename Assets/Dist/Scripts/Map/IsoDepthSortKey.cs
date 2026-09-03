// ============================================================
// IsoDepthSortKey — 이소 투명 정렬 비교 키 (y → x+z → x)
// ============================================================

using System;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// 화면에 그려지는 투명 렌더러 집합을 정렬할 때 쓰는 비교 키.
    /// <see cref="IsoVisibleDepthSortRegistry"/>가 최종 <c>sortingOrder</c> 0..N-1을 부여한다.
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
