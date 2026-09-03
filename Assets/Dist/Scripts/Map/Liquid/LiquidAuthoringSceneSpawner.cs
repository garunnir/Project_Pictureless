// ============================================================
// LiquidAuthoringSceneSpawner — liquidAuthoringFaces → 씬 물 마커 복원 (에디터 전용)
// ============================================================
// 물 마커는 저작 도구다. Play에서는 절대 스폰하지 않는다 — 런타임 수면은
// MapLiquidSurfaceRenderer가 overlay를 보고 그리므로 겹쳐 그리면 안 된다.

using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public static class LiquidAuthoringSceneSpawner
    {
        /// <summary>
        /// 저작 면마다 프리팹을 인스턴스화하고 <see cref="LiquidAuthoringView"/>를 앵커에 맞춥니다.
        /// </summary>
        /// <returns>스폰한 마커 수.</returns>
        public static int SpawnInto(
            Transform parent,
            IReadOnlyList<FloorFaceSaveData> authoringFaces,
            TilePrefabDB prefabDB,
            float cellSize)
        {
            if (parent == null || authoringFaces == null || authoringFaces.Count == 0)
                return 0;

            if (prefabDB == null)
            {
                Debug.LogError(
                    "[LiquidAuthoringSceneSpawner] TilePrefabDB가 없어 물 마커를 복원하지 못했습니다.");
                return 0;
            }

            float cs = Mathf.Max(1e-4f, cellSize);
            int spawned = 0;

            for (int i = 0; i < authoringFaces.Count; i++)
            {
                FloorFaceSaveData face = authoringFaces[i];
                if (face == null || string.IsNullOrEmpty(face.prefabId))
                    continue;

                GameObject prefab = prefabDB.GetPrefab(face.prefabId);
                if (prefab == null)
                {
                    Debug.LogError(
                        $"[LiquidAuthoringSceneSpawner] prefabId='{face.prefabId}' 프리팹을 찾지 못해 " +
                        "물 마커를 건너뜁니다.");
                    continue;
                }

                var anchor = new Vector3Int(face.x, face.y, face.z);
                FloorFaceKey.GetWorldPose(
                    new FloorFaceKey(anchor, FloorFace.PosY),
                    cs,
                    out Vector3 pos,
                    out Quaternion rot);

                GameObject go = TilePrefabSpawnUtil.Instantiate(prefab, parent, pos, rot);
                if (go == null)
                    continue;

                var view = go.GetComponent<LiquidAuthoringView>();
                if (view == null)
                {
                    Debug.LogError(
                        $"[LiquidAuthoringSceneSpawner] 프리팹 '{face.prefabId}'에 LiquidAuthoringView가 없습니다. " +
                        "물 프리팹의 컴포넌트를 TileView에서 LiquidAuthoringView로 교체하세요.",
                        go);
                    continue;
                }

                view.prefabId = face.prefabId;
                view.gizmoCellSize = cs;
                view.gridPos = anchor;
                view.SimulateFlowOnLoad = face.simulateFlow;
                spawned++;
            }

            return spawned;
        }
    }
}
