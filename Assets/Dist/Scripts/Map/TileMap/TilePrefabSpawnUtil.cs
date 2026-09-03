// ============================================================
// TilePrefabSpawnUtil — 타일 프리팹 인스턴스화 (에디터=프리팹 연결, 런타임=Instantiate)
// ============================================================

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IsoTilemap
{
    internal static class TilePrefabSpawnUtil
    {
        public static GameObject Instantiate(
            GameObject prefab,
            Transform parent,
            Vector3 position,
            Quaternion rotation)
        {
            if (prefab == null)
                return null;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                GameObject asset = ResolvePrefabAsset(prefab);
                if (asset != null && PrefabUtility.IsPartOfPrefabAsset(asset))
                {
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
                    if (instance != null)
                    {
                        Transform t = instance.transform;
                        t.SetPositionAndRotation(position, rotation);
                        return instance;
                    }
                }
            }
#endif
            return Object.Instantiate(prefab, position, rotation, parent);
        }

#if UNITY_EDITOR
        static GameObject ResolvePrefabAsset(GameObject prefabOrInstance)
        {
            if (prefabOrInstance == null)
                return null;

            if (PrefabUtility.IsPartOfPrefabAsset(prefabOrInstance))
                return prefabOrInstance;

            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(prefabOrInstance);
            return source != null ? source : prefabOrInstance;
        }
#endif
    }
}
