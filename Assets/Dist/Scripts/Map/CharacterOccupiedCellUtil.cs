// ============================================================
// CharacterOccupiedCellUtil — 캐릭터 grid footprint 점유 셀 해석
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public static class CharacterOccupiedCellUtil
    {
        public static bool TryGetAnchorFromFeet(
            Vector3Int feetCell,
            Vector3Int footprint,
            out Vector3Int anchor)
        {
            footprint = CharacterGridFootprintDefaults.Clamp(footprint);
            int sx = footprint.x;
            int sz = footprint.z;
            anchor = new Vector3Int(
                feetCell.x - (sx - 1) / 2,
                feetCell.y,
                feetCell.z - (sz - 1) / 2);
            return true;
        }

        public static void AppendOccupiedCells(
            Vector3Int feetCell,
            Vector3Int footprint,
            ICollection<Vector3Int> cells)
        {
            if (cells == null)
                return;

            footprint = CharacterGridFootprintDefaults.Clamp(footprint);
            if (!TryGetAnchorFromFeet(feetCell, footprint, out Vector3Int anchor))
                return;

            TileIdentityUtil.AppendOccupiedCellBox(anchor, footprint, cells);
        }

        public static (int minY, int maxY) GetVerticalBand(Vector3Int feetCell, Vector3Int footprint)
        {
            footprint = CharacterGridFootprintDefaults.Clamp(footprint);
            TryGetAnchorFromFeet(feetCell, footprint, out Vector3Int anchor);
            int sy = footprint.y;
            return (anchor.y, anchor.y + sy - 1);
        }

        public static bool Contains(Vector3Int feetCell, Vector3Int footprint, Vector3Int cell)
        {
            footprint = CharacterGridFootprintDefaults.Clamp(footprint);
            if (!TryGetAnchorFromFeet(feetCell, footprint, out Vector3Int anchor))
                return false;

            int sx = footprint.x;
            int sy = footprint.y;
            int sz = footprint.z;
            return cell.x >= anchor.x && cell.x < anchor.x + sx
                && cell.y >= anchor.y && cell.y < anchor.y + sy
                && cell.z >= anchor.z && cell.z < anchor.z + sz;
        }
    }
}
