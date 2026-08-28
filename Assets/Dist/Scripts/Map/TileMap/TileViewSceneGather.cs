using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IsoTilemap
{
    /// <summary>
    /// 씬의 <see cref="MapPlacedView"/>를 저장 레이어로 바꿉니다 —
    /// <see cref="TileView"/>는 <see cref="TileData"/>, <see cref="LiquidAuthoringView"/>는 물 저작 면.
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

                string prefabId = ResolvePrefabId(v);
                if (string.IsNullOrEmpty(prefabId))
                {
                    Debug.LogError(
                        $"[TileViewSceneGather] prefabId를 해석하지 못해 타일을 건너뜁니다: '{v.name}'. " +
                        "TileView.prefabId를 채우거나 TilePrefabDB에 프리팹을 등록하세요.",
                        v);
                    continue;
                }

                var slot = v.placementSlot;
                if (slot == TilePlacementSlot.None &&
                    prefabId.StartsWith("Slope/", StringComparison.Ordinal))
                {
                    slot = TilePlacementSlot.OccupiedCell;
                }

                if (slot == TilePlacementSlot.None)
                    slot = TileIdentityUtil.InferSlotFromPrefabId(prefabId);

                if (slot == TilePlacementSlot.None)
                {
                    if (TilePrefabDB.TryResolveDefinition(prefabId, out TileDefinition slotDef) &&
                        slotDef != null &&
                        slotDef.placementSlot != TilePlacementSlot.None)
                    {
                        slot = slotDef.placementSlot;
                    }
                }

                if (slot == TilePlacementSlot.None) continue;

                if (!TryBakeFromDefinition(prefabId, slot, out var size, out byte collisionFlags))
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
                    PrefabId = prefabId,
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

        /// <summary>
        /// 씬의 물 저작 마커를 <see cref="MapSaveJsonDto.liquidAuthoringFaces"/> 항목으로 바꿉니다.
        /// 앵커는 바닥 +Y 면(CellBelow)이며, 같은 앵커가 겹치면 마지막 하나만 남습니다.
        /// </summary>
        public static List<FloorFaceSaveData> BuildLiquidAuthoringFaces(IEnumerable<LiquidAuthoringView> views)
        {
            var byAnchor = new Dictionary<Vector3Int, (string prefabId, bool simulateFlow)>();

            foreach (var v in views)
            {
                if (v == null) continue;

                string prefabId = ResolvePrefabId(v);
                if (string.IsNullOrEmpty(prefabId))
                {
                    Debug.LogError(
                        $"[TileViewSceneGather] prefabId를 해석하지 못해 물 마커를 건너뜁니다: '{v.name}'. " +
                        "LiquidAuthoringView.prefabId를 채우거나 TilePrefabDB에 프리팹을 등록하세요.",
                        v);
                    continue;
                }

                Vector3Int anchor = v.gridPos;
                float cellSize = Mathf.Max(1e-4f, v.gizmoCellSize);
                if (FloorFacePicker.TryPickNearest(v.transform.position, cellSize, out var nearest))
                    anchor = nearest.Anchor;

                byAnchor[anchor] = (prefabId, v.SimulateFlowOnLoad);
            }

            var faces = new List<FloorFaceSaveData>(byAnchor.Count);
            foreach (var kv in byAnchor)
            {
                faces.Add(new FloorFaceSaveData
                {
                    x = kv.Key.x,
                    y = kv.Key.y,
                    z = kv.Key.z,
                    face = (byte)FloorFace.PosY,
                    prefabId = kv.Value.prefabId,
                    simulateFlow = kv.Value.simulateFlow,
                });
            }

            return faces;
        }

        /// <summary>
        /// Floor/ShallowWater 등 변형 프리팹은 뷰의 prefabId가 비어 있는 경우가 많다.
        /// Prefab 소스 → TilePrefabDB.prefab 참조로 역조회한다.
        /// </summary>
        static string ResolvePrefabId(MapPlacedView view)
        {
            if (!string.IsNullOrEmpty(view.prefabId))
                return view.prefabId;

#if UNITY_EDITOR
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(view.gameObject);
            if (source != null &&
                TilePrefabDB.TryResolvePrefabIdByPrefab(source, out string fromDb) &&
                !string.IsNullOrEmpty(fromDb))
            {
                return fromDb;
            }

            if (source != null)
                return UnityEditor.Tile.PrefabDBExtensions.GetTilePrefabName(source);

            return UnityEditor.Tile.PrefabDBExtensions.GetTilePrefabName(view.gameObject);
#else
            return null;
#endif
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
