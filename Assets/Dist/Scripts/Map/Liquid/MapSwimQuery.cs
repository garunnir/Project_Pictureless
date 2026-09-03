// ============================================================
// MapSwimQuery — 발밑 immersion·수면/컬럼 Y 조회 SSOT
// ============================================================

using UnityEngine;

namespace IsoTilemap
{
    public static class MapSwimQuery
    {
        /// <summary>
        /// 발 월드 → immersion. <paramref name="diveHeld"/>면 Swim 가능 시 Dive.
        /// 얼음 바닥은 Dry. 셀은 OccupiedCellCoord / MapPlantHost와 동일 경로.
        /// </summary>
        public static MapSwimImmersion Resolve(
            Vector3 feetWorld,
            float cellSize,
            bool diveHeld,
            Vector3Int gridFootprint = default)
        {
            if (cellSize <= 0f)
                cellSize = 1f;

            Vector3Int feetCell = ResolveFeetCell(feetWorld, cellSize);
            Vector3Int supportCell = new Vector3Int(feetCell.x, feetCell.y - 1, feetCell.z);
            if (MapLiquidQuery.ProvidesSolidSupport(supportCell))
                return MapSwimImmersion.DryDefault;

            float fill01 = MapLiquidQuery.Fill01(feetCell);
            if (MapLiquidQuery.IsSolid(feetCell))
                return MapSwimImmersion.DryDefault;

            int columnMl = MapLiquidQuery.ColumnMlDownward(feetCell);
            ResolveColumnBounds(
                feetCell,
                cellSize,
                out float surfaceFeetY,
                out float bottomFeetY);

            Vector3 headWorld = feetWorld + Vector3.up * ResolveHeadHeight(gridFootprint, cellSize);
            Vector3Int headCell = ResolveFeetCell(headWorld, cellSize);
            bool headSubmerged = MapLiquidQuery.Fill01(headCell) >= MapSwimConsts.HeadSubmergeFill01
                && !MapLiquidQuery.IsSolid(headCell);

            bool canSwim = columnMl >= MapSwimConsts.SwimColumnMl;
            MapSwimMode mode;
            if (fill01 < MapSwimConsts.WadeFill01 && !canSwim)
            {
                mode = MapSwimMode.Dry;
            }
            else if (!canSwim)
            {
                mode = MapSwimMode.Wade;
            }
            else if (diveHeld || headSubmerged)
            {
                mode = MapSwimMode.Dive;
            }
            else
            {
                mode = MapSwimMode.Swim;
            }

            return new MapSwimImmersion(
                mode,
                feetCell,
                fill01,
                columnMl,
                surfaceFeetY,
                bottomFeetY,
                canSwim,
                headSubmerged);
        }

        public static float SurfaceLift01(float fill01)
        {
            float lift01 = Mathf.Max(fill01, MapLiquidRenderConsts.SurfaceMinLift01);
            float ceiling = 1f - MapLiquidRenderConsts.SurfaceTopInset01;
            if (lift01 > ceiling)
                lift01 = ceiling;
            return lift01;
        }

        public static float CellSurfaceFeetY(Vector3Int cell, float cellSize)
        {
            if (cellSize <= 0f)
                cellSize = 1f;

            float fill01 = MapLiquidQuery.Fill01(cell);
            return cell.y * cellSize + SurfaceLift01(fill01) * cellSize;
        }

        static void ResolveColumnBounds(
            Vector3Int feetCell,
            float cellSize,
            out float surfaceFeetY,
            out float bottomFeetY)
        {
            Vector3Int top = feetCell;
            for (int i = 0; i < MapLiquidConsts.MaxColumnScanCells; i++)
            {
                var above = new Vector3Int(top.x, top.y + 1, top.z);
                if (MapLiquidQuery.GetEffectiveMl(above) <= 0)
                    break;
                top = above;
            }

            surfaceFeetY = CellSurfaceFeetY(top, cellSize);

            Vector3Int bottom = feetCell;
            for (int i = 0; i < MapLiquidConsts.MaxColumnScanCells; i++)
            {
                var below = new Vector3Int(bottom.x, bottom.y - 1, bottom.z);
                if (MapLiquidQuery.GetEffectiveMl(below) <= 0)
                    break;
                bottom = below;
            }

            bottomFeetY = bottom.y * cellSize;
        }

        static float ResolveHeadHeight(Vector3Int gridFootprint, float cellSize)
        {
            gridFootprint = CharacterGridFootprintDefaults.Clamp(
                gridFootprint == Vector3Int.zero
                    ? CharacterGridFootprintDefaults.Default
                    : gridFootprint);

            if (gridFootprint.y > 0 && cellSize > 0f)
                return gridFootprint.y * cellSize;

            return MapSwimConsts.HeadHeightWorld;
        }

        static Vector3Int ResolveFeetCell(Vector3 feetWorld, float cellSize)
        {
            MapPlantHost plant = MapPlantHost.Runtime;
            if (plant != null)
                return plant.ResolveCellFromWorld(feetWorld);

            TileMapCacheHub hub = TileMapCacheHub.Runtime;
            if (hub != null)
                return OccupiedCellCoord.ResolveFromWorld(hub, feetWorld, cellSize);

            return TileHelper.ConvertWorldToGrid(feetWorld, cellSize);
        }
    }
}
