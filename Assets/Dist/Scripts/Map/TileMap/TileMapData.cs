using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace IsoTilemap
{
    public static class TileHelper
    {
        /// <summary>
        /// <see cref="ConvertGridToWorldPos(Vector3,float)"/>의 역변환과 맞춘 월드→그리드 변환입니다.
        /// </summary>
        public static Vector3Int ConvertWorldToGrid(Vector3 worldPos, float cellSize)
        {
            if (cellSize <= 0f) cellSize = 1f;
            return new Vector3Int(
                Mathf.RoundToInt(worldPos.x / cellSize - 0.5f),
                Mathf.RoundToInt(worldPos.y / cellSize),
                Mathf.RoundToInt(worldPos.z / cellSize - 0.5f)
            );
        }

        public static Vector3Int ConvertWorldToGrid(Vector3 worldPos) =>
            ConvertWorldToGrid(worldPos, 1f);
        public static Vector3 ConvertGridToWorldPos(Vector3Int gridPos, float cellSize = 1f)
        {
            return ConvertGridToWorldPos((Vector3)gridPos, cellSize);
        }
        public static Vector3 ConvertGridToWorldPos(Vector3 worldPos, float cellSize = 1f)
        {
            Vector3 wPos = new Vector3(
            worldPos.x * cellSize + 0.5f * cellSize,
            worldPos.y * cellSize,
                worldPos.z * cellSize + 0.5f * cellSize
);
            return wPos;
        }

        /// <summary>
        /// 카드널 인접 두 셀 사이 면의 월드 중점 (수직 벽 EdgeWall용).
        /// X/Z는 <see cref="ConvertGridToWorldPos"/> 셀 중심, Y는 동일 층이므로 셀 바닥.
        /// </summary>
        public static Vector3 GetAdjacentCellFaceMidpoint(Vector3Int cellA, Vector3Int cellB, float cellSize = 1f)
        {
            if (cellSize <= 0f) cellSize = 1f;
            Vector3 a = ConvertGridToWorldPos(cellA, cellSize);
            Vector3 b = ConvertGridToWorldPos(cellB, cellSize);
            return (a + b) * 0.5f;
        }

        /// <summary>
        /// 수직 인접 두 셀 사이 수평 바닥면 pose. Y는 격자 경계(<see cref="cellAbove"/> 바닥), X/Z는 셀 열 중심.
        /// </summary>
        public static Vector3 GetHorizontalCellFaceWorldPos(
            Vector3Int cellBelow,
            Vector3Int cellAbove,
            float cellSize = 1f)
        {
            if (cellSize <= 0f) cellSize = 1f;
            Vector3 below = ConvertGridToWorldPos(cellBelow, cellSize);
            float boundaryY = ConvertGridToWorldPos(cellAbove, cellSize).y;
            return new Vector3(below.x, boundaryY, below.z);
        }

        public static void GetOccupiedCellWireBox(
            Vector3Int cell,
            float cellSize,
            Vector3Int sizeUnits,
            out Vector3 center,
            out Vector3 size)
        {
            if (cellSize <= 0f)
                cellSize = 1f;

            int sx = Mathf.Max(1, sizeUnits.x);
            int sy = Mathf.Max(1, sizeUnits.y);
            int sz = Mathf.Max(1, sizeUnits.z);
            size = new Vector3(sx * cellSize, sy * cellSize, sz * cellSize);
            Vector3 origin = ConvertGridToWorldPos(cell, cellSize);
            center = new Vector3(origin.x, origin.y + size.y * 0.5f, origin.z);
        }

        public static void DrawOccupiedCellWire(
            Vector3Int cell,
            float cellSize,
            Color color,
            Vector3Int sizeUnits = default)
        {
            if (sizeUnits == Vector3Int.zero)
                sizeUnits = Vector3Int.one;

            GetOccupiedCellWireBox(cell, cellSize, sizeUnits, out Vector3 center, out Vector3 size);
            Color previous = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawWireCube(center, size);
            Gizmos.color = previous;
        }
    }

}
