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
        /// tileType이 none이 아닌 뷰만 포함합니다. 새 export용 <see cref="TileData.tileDefId"/>는 매번 새로 만듭니다.
        /// </summary>
        public static List<TileData> BuildTileDataSnapshot(IEnumerable<TileView> views)
        {
            var list = new List<TileData>();
            foreach (var v in views)
            {
                if (v == null) continue;

                // Slope prefabs는 과거에 tileType이 none으로 저장된 경우가 있어,
                // export 시 prefabId로 다시 승격해서 저장되도록 방어합니다.
                var tileType = v.tileType;
                if (tileType == TileView.TileType.none &&
                    !string.IsNullOrEmpty(v.prefabId) &&
                    v.prefabId.StartsWith("Slope/", StringComparison.Ordinal))
                {
                    tileType = TileView.TileType.Slope;
                }

                if (tileType == TileView.TileType.none) continue;

                if (!TryBakeFromDefinition(v.prefabId, (byte)tileType, out var size, out byte collisionFlags))
                    continue;

                byte t = (byte)tileType;
                byte ef = TileIdentity.EdgeFaceNone;
                Vector3Int grid = v.gridPos;

                if (tileType == TileView.TileType.EdgeWall)
                {
                    ef = (byte)Mathf.Clamp(v.wallEdgeFace, 0, 1);
                }

                list.Add(new TileData
                {
                    tileDefId = Guid.NewGuid(),
                    state = default,
                    identity = new TileIdentity
                    {
                        PrefabId = v.prefabId ?? string.Empty,
                        GridPos = grid,
                        sizeUnit = size,
                        tileType = t,
                        edgeFace = ef,
                        collisionFlags = collisionFlags,
                    }
                });
            }

            return list;
        }

        static bool TryBakeFromDefinition(
            string prefabId,
            byte tileType,
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
            collisionFlags = TileCollisionProfile.FromDefinitionForTileType(tileType, def);
            return true;
        }
    }
}
