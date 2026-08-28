// ============================================================
// MapLiquidAuthoringBake — 물 저작 면(liquidAuthoringFaces) → liquidCells 베이크
// ============================================================
// 물은 타일 모델에 진입하지 않는다. 씬의 LiquidAuthoringView가 liquidAuthoringFaces로 저장되고,
// 편집 모드 저장 시(MapLiquidHost가 없을 때) 이 클래스가 walkable 셀(CellAbove) liquidCells로 굽는다.
// Play 중 저장은 MapLiquidHost.WriteToDto가 이기고 이 bake는 호출되지 않는다.
//
// 구 JSON은 물이 floorFaces에 Floor 타일로 들어 있다 — PromoteLegacyFloorFaces가 read 경계에서
// 한 번 저작 면으로 옮기므로, 이후 경로(DtoMapper·시드·bake)는 liquidAuthoringFaces만 본다.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public static class MapLiquidAuthoringBake
    {
        /// <summary>
        /// <paramref name="def"/>가 워터 태그면 시드 ml을 반환한다.
        /// Shallow/Deep는 **같은 물** — 구분 없이 cap 가득. (깊이·얕음은 ml/컬럼으로만 표현)
        /// </summary>
        public static bool TryResolveSeedMl(TileDefinition def, out int seedMl)
        {
            seedMl = 0;
            if (def == null)
                return false;

            if (TileFlags.HasFlag(def, TileFlags.DeepWater)
                || TileFlags.HasFlag(def, TileFlags.ShallowWater))
            {
                seedMl = MapLiquidConsts.DefaultMaxVolumeMl;
                return true;
            }

            return false;
        }

        /// <summary>prefabId가 물 저작 대상인지 — 타일 모델 진입을 막는 게이트로도 쓴다.</summary>
        public static bool IsLiquidAuthoringPrefab(string prefabId) =>
            TilePrefabDB.TryResolveDefinition(prefabId, out TileDefinition def)
            && TryResolveSeedMl(def, out _);

        /// <summary>
        /// 구 JSON의 워터 Floor face를 <see cref="MapSaveJsonDto.liquidAuthoringFaces"/>로 옮긴다(one-way).
        /// 옮긴 항목은 floorFaces에서 제거해 Floor 타일로 승격되지 않게 한다.
        /// </summary>
        /// <returns>옮긴 face 수.</returns>
        public static int PromoteLegacyFloorFaces(MapSaveJsonDto dto)
        {
            if (dto?.floorFaces == null || dto.floorFaces.Count == 0)
                return 0;

            dto.liquidAuthoringFaces ??= new List<FloorFaceSaveData>();

            int promoted = 0;
            for (int i = dto.floorFaces.Count - 1; i >= 0; i--)
            {
                FloorFaceSaveData face = dto.floorFaces[i];
                if (face == null || string.IsNullOrEmpty(face.prefabId))
                    continue;

                if (!IsLiquidAuthoringPrefab(face.prefabId))
                    continue;

                dto.floorFaces.RemoveAt(i);
                dto.liquidAuthoringFaces.Add(face);
                promoted++;
            }

            if (promoted > 0)
            {
                Debug.Log(
                    $"[MapLiquidAuthoringBake] 구 JSON 워터 floor face {promoted}개를 " +
                    "liquidAuthoringFaces로 승격했습니다. 다음 저장에서 새 레이어로 기록됩니다.");
            }

            return promoted;
        }

        /// <summary>
        /// 저작 면을 (액체 셀, 시드 ml)로 펼친다. JSON x,y,z = CellBelow 앵커이므로
        /// 액체는 walkable CellAbove에 담긴다. 같은 셀이 겹치면 마지막 하나만 남는다.
        /// </summary>
        public static Dictionary<Vector3Int, int> ResolveAuthoringCells(
            IReadOnlyList<FloorFaceSaveData> authoringFaces)
        {
            var byCell = new Dictionary<Vector3Int, int>();
            if (authoringFaces == null)
                return byCell;

            int unresolvedWaterLooks = 0;

            for (int i = 0; i < authoringFaces.Count; i++)
            {
                FloorFaceSaveData face = authoringFaces[i];
                if (face == null || string.IsNullOrEmpty(face.prefabId))
                    continue;

                if (!TilePrefabDB.TryResolveDefinition(face.prefabId, out TileDefinition def))
                {
                    if (LooksLikeWaterPrefabId(face.prefabId))
                        unresolvedWaterLooks++;
                    continue;
                }

                if (!TryResolveSeedMl(def, out int seedMl))
                    continue;

                byCell[new Vector3Int(face.x, face.y + 1, face.z)] = seedMl;
            }

            if (unresolvedWaterLooks > 0)
            {
                Debug.LogError(
                    $"[MapLiquidAuthoringBake] 워터처럼 보이는 저작 면 {unresolvedWaterLooks}개가 " +
                    "TilePrefabDB에 없어 무시되었습니다. prefabId를 확인하세요.");
            }

            return byCell;
        }

        /// <summary>
        /// <paramref name="dto"/>.liquidAuthoringFaces로 liquidCells를 덮어쓴다.
        /// 저작 면이 하나도 없으면 false — 호출부가 기존 liquidCells를 계승해야 한다.
        /// </summary>
        public static bool TryBakeFromAuthoringFaces(MapSaveJsonDto dto, out int bakedCellCount)
        {
            bakedCellCount = 0;
            if (dto == null)
                return false;

            Dictionary<Vector3Int, int> byCell = ResolveAuthoringCells(dto.liquidAuthoringFaces);
            if (byCell.Count == 0)
                return false;

            dto.liquidCells ??= new List<MapLiquidCellSaveData>();
            dto.liquidCells.Clear();

            foreach (var kv in byCell)
            {
                MapLiquidCell cell = MapLiquidCell.FromEffectiveMl(
                    MapLiquidConsts.WaterTypeId,
                    kv.Value,
                    MapLiquidAmbient.ResolveDeciC(kv.Key));
                dto.liquidCells.Add(new MapLiquidCellSaveData
                {
                    x = kv.Key.x,
                    y = kv.Key.y,
                    z = kv.Key.z,
                    typeId = cell.TypeId,
                    level = cell.Level,
                    remainderMl = cell.RemainderMl,
                    tempDeciC = cell.TempDeciC,
                });
            }

            dto.hasLiquidSnapshot = true;
            dto.hasLiquidTemperature = true;
            bakedCellCount = dto.liquidCells.Count;
            return true;
        }

        static bool LooksLikeWaterPrefabId(string prefabId) =>
            prefabId.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
