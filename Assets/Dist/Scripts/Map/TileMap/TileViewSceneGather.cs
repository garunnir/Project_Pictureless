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

                byte t = (byte)tileType;
                byte ef = TileIdentity.EdgeFaceNone;
                Vector3Int size = ResolveSizeFromDefinition(v.prefabId, tileType);
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
                        edgeFace = ef
                    }
                });
            }

            return list;
        }

        static Vector3Int ResolveSizeFromDefinition(string prefabId, TileView.TileType tileType)
        {
            if (TilePrefabDB.TryResolveDefinitionSize(prefabId, out var size))
                return size;

            // 정의가 없으면 기존 기본값을 유지합니다.
            if (tileType == TileView.TileType.EdgeWall)
                return Vector3Int.one;

            return Vector3Int.one;
        }
    }
}
