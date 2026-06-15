using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// 씬의 <see cref="TileView"/>를 <see cref="TileData"/> 목록으로 바꿔 모델 초기화·JSON 저장에 씁니다.
    /// </summary>
    public static class TileViewSceneGather
    {
        /// <summary>
        /// placementSlot이 None이 아닌 뷰만 포함합니다. 새 export용 <see cref="TileData.tileDefId"/>는 매번 새로 만듭니다.
        /// </summary>
        public static List<TileData> BuildTileDataSnapshot(IEnumerable<TileView> views)
        {
            var list = new List<TileData>();
            foreach (var v in views)
            {
                if (v == null) continue;

                var slot = v.placementSlot;
                if (slot == TilePlacementSlot.None &&
                    !string.IsNullOrEmpty(v.prefabId) &&
                    v.prefabId.StartsWith("Slope/", StringComparison.Ordinal))
                {
                    slot = TilePlacementSlot.OccupiedCell;
                }

                if (slot == TilePlacementSlot.None &&
                    !string.IsNullOrEmpty(v.prefabId))
                {
                    slot = TileIdentityUtil.InferSlotFromPrefabId(v.prefabId);
                }

                if (slot == TilePlacementSlot.None) continue;

                if (!TryBakeFromDefinition(v.prefabId, slot, out var size, out byte collisionFlags))
                    continue;

                byte wallFace = slot == TilePlacementSlot.VerticalFace
                    ? (byte)Mathf.Clamp(v.wallFace, 0, 1)
                    : (byte)0;
                byte floorFace = slot == TilePlacementSlot.HorizontalFace
                    ? (byte)FloorFace.PosY
                    : (byte)0;

                Vector3Int gridPos = v.gridPos;
                if (slot == TilePlacementSlot.HorizontalFace)
                {
                    float cellSize = Mathf.Max(1e-4f, v.gizmoCellSize);
                    if (FloorFacePicker.TryPickNearest(v.transform.position, cellSize, out var nearest))
                        gridPos = nearest.Anchor;
                }

                var identity = new TileIdentity
                {
                    PrefabId = v.prefabId ?? string.Empty,
                    GridPos = gridPos,
                    sizeUnit = size,
                    placementSlot = (byte)slot,
                    wallFace = wallFace,
                    floorFace = floorFace,
                    collisionFlags = collisionFlags,
                };

                list.Add(new TileData
                {
                    tileDefId = Guid.NewGuid(),
                    state = default,
                    identity = identity,
                });
            }

            return list;
        }

        static bool TryBakeFromDefinition(
            string prefabId,
            TilePlacementSlot slot,
            out Vector3Int sizeUnit,
            out byte collisionFlags)
        {
            sizeUnit = Vector3Int.one;
            collisionFlags = 0;

            if (!TilePrefabDB.TryResolveDefinition(prefabId, out var def) || def == null)
            {
                Debug.LogError($"[TileViewSceneGather] Definition not found for prefabId='{prefabId}'. Tile skipped.");
                return false;
            }

            sizeUnit = new Vector3Int(
                Mathf.Max(1, def.size.x),
                Mathf.Max(1, def.size.y),
                Mathf.Max(1, def.size.z));
            collisionFlags = TileCollisionProfile.FromDefinitionForSlot(slot, def);
            return true;
        }
    }
}
