// ============================================================
// MapPlantVisualHierarchy — Mesh/Sprite 자식 분리 (같은 GO 충돌 방지)
// ============================================================

using UnityEngine;
using UnityEngine.Rendering;

namespace IsoTilemap
{
    public static class MapPlantVisualHierarchy
    {
        public static void EnsureChildren(
            Transform root,
            out MeshFilter filter,
            out MeshRenderer meshRenderer,
            out SpriteRenderer spriteRenderer)
        {
            filter = null;
            meshRenderer = null;
            spriteRenderer = null;
            if (root == null)
                return;

            Transform meshTf = root.Find(MapPlantConsts.MeshVisualChildName);
            if (meshTf == null)
            {
                var meshGo = new GameObject(MapPlantConsts.MeshVisualChildName);
                meshTf = meshGo.transform;
                meshTf.SetParent(root, false);
            }

            if (!meshTf.TryGetComponent(out filter))
                filter = meshTf.gameObject.AddComponent<MeshFilter>();
            if (!meshTf.TryGetComponent(out meshRenderer))
                meshRenderer = meshTf.gameObject.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            Transform spriteTf = root.Find(MapPlantConsts.SpriteVisualChildName);
            if (spriteTf == null)
            {
                var spriteGo = new GameObject(MapPlantConsts.SpriteVisualChildName);
                spriteTf = spriteGo.transform;
                spriteTf.SetParent(root, false);
            }

            if (!spriteTf.TryGetComponent(out spriteRenderer))
                spriteRenderer = spriteTf.gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            if (spriteRenderer.sprite == null)
                spriteRenderer.enabled = false;
        }

        public static void CacheFromRoot(
            Transform root,
            out MeshFilter filter,
            out MeshRenderer meshRenderer,
            out SpriteRenderer spriteRenderer)
        {
            filter = null;
            meshRenderer = null;
            spriteRenderer = null;
            if (root == null)
                return;

            Transform meshTf = root.Find(MapPlantConsts.MeshVisualChildName);
            if (meshTf != null)
            {
                meshTf.TryGetComponent(out filter);
                meshTf.TryGetComponent(out meshRenderer);
            }

            Transform spriteTf = root.Find(MapPlantConsts.SpriteVisualChildName);
            if (spriteTf != null)
                spriteTf.TryGetComponent(out spriteRenderer);
        }
    }
}
