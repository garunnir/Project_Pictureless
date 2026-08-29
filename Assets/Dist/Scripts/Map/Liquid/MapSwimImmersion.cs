// ============================================================
// MapSwimImmersion — 발밑 액체 immersion 스냅샷 (모드·ml·수면)
// ============================================================

using UnityEngine;

namespace IsoTilemap
{
    public enum MapSwimMode : byte
    {
        Dry = 0,
        Wade = 1,
        Swim = 2,
        Dive = 3
    }

    public readonly struct MapSwimImmersion
    {
        public readonly MapSwimMode Mode;
        public readonly Vector3Int FeetCell;
        public readonly float Fill01;
        public readonly int ColumnMl;
        public readonly float SurfaceFeetY;
        public readonly float ColumnBottomFeetY;
        public readonly bool CanSwim;
        public readonly bool HeadSubmerged;

        public MapSwimImmersion(
            MapSwimMode mode,
            Vector3Int feetCell,
            float fill01,
            int columnMl,
            float surfaceFeetY,
            float columnBottomFeetY,
            bool canSwim,
            bool headSubmerged)
        {
            Mode = mode;
            FeetCell = feetCell;
            Fill01 = fill01;
            ColumnMl = columnMl;
            SurfaceFeetY = surfaceFeetY;
            ColumnBottomFeetY = columnBottomFeetY;
            CanSwim = canSwim;
            HeadSubmerged = headSubmerged;
        }

        public static MapSwimImmersion DryDefault =>
            new MapSwimImmersion(MapSwimMode.Dry, default, 0f, 0, 0f, 0f, false, false);
    }
}
