using UnityEngine;

namespace IsoTilemap
{
    /// <summary>topology 충돌·지지에서 쓰는 월드↔그리드 변환. 타일 <see cref="TileHelper.ConvertWorldToGrid"/>와 동일 규칙.</summary>
    public static class MapCollisionGrid
    {
        public readonly struct FeetCell
        {
            public readonly Vector3 FeetWorld;
            public readonly float FeetY;
            public readonly int X;
            public readonly int Z;
            public readonly int GridY;

            public FeetCell(Vector3 feetWorld, int x, int z, int gridY)
            {
                FeetWorld = feetWorld;
                FeetY = feetWorld.y;
                X = x;
                Z = z;
                GridY = gridY;
            }
        }

        public static int WorldToGridY(Vector3 world, float cellSize) =>
            TileHelper.ConvertWorldToGrid(world, cellSize).y;

        public static float GridYToSurfaceY(int gridY, float cellSize) =>
            gridY * cellSize;

        public static FeetCell ResolveFeetCell(Vector3 bodyWorld, float feetOffset, float cellSize)
        {
            float feetY = bodyWorld.y - feetOffset;
            var feetWorld = new Vector3(bodyWorld.x, feetY, bodyWorld.z);
            var cell = TileHelper.ConvertWorldToGrid(feetWorld, cellSize);
            return new FeetCell(feetWorld, cell.x, cell.z, cell.y);
        }

        public static Vector3Int ToGrid(FeetCell feet) =>
            new Vector3Int(feet.X, feet.GridY, feet.Z);

        /// <summary>수직 이동만 있을 때 X/Z 그리드는 유지하고 Y만 갱신합니다.</summary>
        public static FeetCell WithFeetY(FeetCell feet, float feetY, float cellSize)
        {
            if (cellSize <= 0f)
                cellSize = 1f;

            var feetWorld = new Vector3(feet.FeetWorld.x, feetY, feet.FeetWorld.z);
            int gridY = Mathf.RoundToInt(feetY / cellSize);
            return new FeetCell(feetWorld, feet.X, feet.Z, gridY);
        }
    }
}
