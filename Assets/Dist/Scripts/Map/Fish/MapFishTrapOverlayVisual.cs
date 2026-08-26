// ============================================================
// MapFishTrapOverlayVisual — 물 셀 통발 오버레이 primitive SSOT
// ============================================================

using UnityEngine;
using UnityEngine.Rendering;

namespace IsoTilemap
{
    public static class MapFishTrapOverlayVisual
    {
        static Mesh _mesh;
        static Material _material;

        public static void Apply(Transform transform, Vector3Int cell, float cellSize)
        {
            EnsureAssets();
            if (transform == null)
                return;

            transform.localScale = MapFishConsts.TrapOverlayScale;
            Vector3 pos = TileHelper.ConvertGridToWorldPos(cell, cellSize);
            pos.y += MapPlantConsts.OverlayYOffset;
            transform.position = pos;

            if (!transform.TryGetComponent(out MeshFilter filter))
                filter = transform.gameObject.AddComponent<MeshFilter>();
            if (!transform.TryGetComponent(out MeshRenderer renderer))
                renderer = transform.gameObject.AddComponent<MeshRenderer>();

            filter.sharedMesh = _mesh;
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            EnsureCollider(transform.gameObject);
        }

        static void EnsureCollider(GameObject go)
        {
            if (go.TryGetComponent(out BoxCollider existing))
            {
                existing.size = Vector3.one;
                existing.center = Vector3.zero;
                return;
            }

            var collider = go.AddComponent<BoxCollider>();
            collider.size = Vector3.one;
            collider.center = Vector3.zero;
        }

        static void EnsureAssets()
        {
            if (_mesh == null)
            {
                GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                if (temp.TryGetComponent(out MeshFilter filter))
                    _mesh = filter.sharedMesh;
                Object.Destroy(temp);
            }

            if (_material != null)
                return;

            Shader shader = Shader.Find(MapPlantConsts.OverlayShaderUrpUnlit);
            if (shader == null)
                shader = Shader.Find(MapPlantConsts.OverlayShaderUnlitColor);
            if (shader == null)
                return;

            _material = new Material(shader) { name = "MapFishTrapOverlay" };
            if (_material.HasProperty("_BaseColor"))
                _material.SetColor("_BaseColor", MapFishConsts.TrapOverlayColor);
            else if (_material.HasProperty("_Color"))
                _material.SetColor("_Color", MapFishConsts.TrapOverlayColor);
        }
    }
}
