// ============================================================
// MapLiquidAuthoringBake — 에디터 워터 floor face → liquidCells 베이크
// ============================================================
// 편집 모드에는 MapLiquidHost가 없으므로, Save Map To JSON 시
// SHALLOW_WATER/DEEP_WATER floorFaces를 walkable 셀(CellAbove) liquidCells로 변환한다.
// Play 중 저장은 MapLiquidHost.WriteToDto가 이기고 이 bake는 호출되지 않는다.

using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public static class MapLiquidAuthoringBake
    {
        /// <summary>
        /// <paramref name="def"/>가 워터 태그면 시드 ml을 반환한다.
        /// Deep가 Shallow보다 우선(둘 다 있으면 Deep).
        /// </summary>
        public static bool TryResolveSeedMl(TileDefinition def, out int seedMl)
        {
            seedMl = 0;
            if (def == null)
                return false;

            if (TileFlags.HasFlag(def, TileFlags.DeepWater))
            {
                seedMl = MapLiquidConsts.DefaultMaxVolumeMl;
                return true;
            }

            if (TileFlags.HasFlag(def, TileFlags.ShallowWater))
            {
                seedMl = Mathf.RoundToInt(
                    MapLiquidConsts.DefaultMaxVolumeMl * MapLiquidConsts.ShallowSeedFraction);
                return true;
            }

            return false;
        }

        /// <summary>
        /// <paramref name="dto"/>.floorFaces에서 워터 태그를 찾아 liquidCells를 덮어쓴다.
        /// 워터 face가 하나도 없으면 false — 호출부가 기존 liquidCells를 계승해야 한다.
        /// </summary>
        public static bool TryBakeFromFloorFaces(MapSaveJsonDto dto, out int bakedCellCount)
        {
            bakedCellCount = 0;
            if (dto?.floorFaces == null || dto.floorFaces.Count == 0)
                return false;

            // 같은 walkable 셀에 shallow+deep가 겹치면 Deep(더 큰 ml)을 남긴다.
            var byCell = new Dictionary<Vector3Int, int>();

            for (int i = 0; i < dto.floorFaces.Count; i++)
            {
                FloorFaceSaveData face = dto.floorFaces[i];
                if (face == null || string.IsNullOrEmpty(face.prefabId))
                    continue;

                if (!TilePrefabDB.TryResolveDefinition(face.prefabId, out TileDefinition def))
                    continue;

                if (!TryResolveSeedMl(def, out int seedMl))
                    continue;

                // JSON x,y,z = CellBelow 앵커. 액체는 walkable CellAbove에 둔다.
                var walkable = new Vector3Int(face.x, face.y + 1, face.z);
                if (byCell.TryGetValue(walkable, out int existing) && existing >= seedMl)
                    continue;

                byCell[walkable] = seedMl;
            }

            if (byCell.Count == 0)
                return false;

            dto.liquidCells ??= new List<MapLiquidCellSaveData>();
            dto.liquidCells.Clear();

            foreach (var kv in byCell)
            {
                MapLiquidCell cell = MapLiquidCell.FromEffectiveMl(
                    MapLiquidConsts.WaterTypeId,
                    kv.Value);
                dto.liquidCells.Add(new MapLiquidCellSaveData
                {
                    x = kv.Key.x,
                    y = kv.Key.y,
                    z = kv.Key.z,
                    typeId = cell.TypeId,
                    level = cell.Level,
                    remainderMl = cell.RemainderMl,
                });
            }

            dto.hasLiquidSnapshot = true;
            bakedCellCount = dto.liquidCells.Count;
            return true;
        }
    }
}
