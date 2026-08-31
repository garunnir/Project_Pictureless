// ============================================================
// CellTargetPreview3D — 셀 타겟 3D 프리뷰 SSOT (Farm plant · 건설 고스트)
// ============================================================

using PlantGrowthStage = global::Garunnir.Runtime.Gameplay.Data.PlantGrowthStage;
using UnityEngine;
using UnityEngine.Rendering;

namespace IsoTilemap
{
    public sealed class CellTargetPreview3D
    {
        public enum ContentMode
        {
            None = 0,
            Plant = 1,
            TileGhost = 2,
        }

        GameObject _root;
        MeshFilter _filter;
        MeshRenderer _meshRenderer;
        SpriteRenderer _spriteRenderer;
        Material _tintMaterial;
        GameObject _tileGhostInstance;
        ContentMode _mode;
        int _facingQuarters;
        bool _rotationEnabled;
        TilePlacementSlot _tileSlot = TilePlacementSlot.OccupiedCell;

        public int FacingQuarters => _facingQuarters;
        public bool IsVisible => _root != null && _root.activeSelf;

        public void BeginPlantMode()
        {
            ClearTileGhost();
            _mode = ContentMode.Plant;
            _rotationEnabled = false;
            _facingQuarters = 0;
            EnsureRoot("CellTargetPreview3D_Plant");
            Hide();
        }

        public void BeginTileGhostMode(TilePlacementSlot slot)
        {
            _mode = ContentMode.TileGhost;
            _tileSlot = slot;
            _rotationEnabled = slot != TilePlacementSlot.HorizontalFace;
            _facingQuarters = 0;
            EnsureRoot("CellTargetPreview3D_Tile");
            Hide();
        }

        public void SetTileGhostPrefab(GameObject prefabSource)
        {
            ClearTileGhost();
            if (_root == null || prefabSource == null)
                return;

            _tileGhostInstance = Object.Instantiate(prefabSource, _root.transform);
            _tileGhostInstance.name = "TileGhost";
            _tileGhostInstance.transform.localPosition = Vector3.zero;
            _tileGhostInstance.transform.localRotation = Quaternion.identity;

            DisableCollidersAndScripts(_tileGhostInstance);
            ApplyTintToRenderers(_tileGhostInstance, ConstructionConsts.TargetPreviewValid);

            if (_filter != null)
                _filter.gameObject.SetActive(false);
            if (_spriteRenderer != null)
                _spriteRenderer.enabled = false;
        }

        public void ShowPlant(
            Vector3Int cell,
            float cellSize,
            PlantGrowthStage stage,
            string seedItemId,
            bool canApply)
        {
            EnsureRoot("CellTargetPreview3D_Plant");
            Color tint = canApply
                ? ConstructionConsts.TargetPreviewValid
                : ConstructionConsts.TargetPreviewInvalid;

            MapPlantOverlayVisual.Apply(
                _root.transform,
                _filter,
                _meshRenderer,
                _spriteRenderer,
                cell,
                cellSize,
                stage,
                seedItemId);
            ApplyPlantTint(tint);
            _root.SetActive(true);
        }

        public void ShowTileAtCell(Vector3Int cell, float cellSize, bool canApply)
        {
            EnsureRoot("CellTargetPreview3D_Tile");
            Vector3 world = TileHelper.ConvertGridToWorldPos(cell, cellSize);
            Quaternion rot = ResolvePreviewRotation();

            if (_tileSlot == TilePlacementSlot.VerticalFace)
            {
                WallEdgeKey key = new WallEdgeKey(cell, (WallFace)(_facingQuarters & 1));
                WallEdgeKey.GetWorldPose(key, cellSize, out world, out rot);
            }

            _root.transform.SetPositionAndRotation(world, rot);
            Color tint = canApply
                ? ConstructionConsts.TargetPreviewValid
                : ConstructionConsts.TargetPreviewInvalid;

            if (_tileGhostInstance != null)
                ApplyTintToRenderers(_tileGhostInstance, tint);
            else
                ApplyPlantTint(tint);

            _root.SetActive(true);
        }

        public void RotateStep(int delta = 1)
        {
            if (!_rotationEnabled)
                return;

            if (_tileSlot == TilePlacementSlot.VerticalFace)
                _facingQuarters = (_facingQuarters + delta) & 1;
            else
                _facingQuarters = (_facingQuarters + delta) & 3;
        }

        public void Hide()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        public void Dispose()
        {
            ClearTileGhost();
            if (_tintMaterial != null)
            {
                Object.Destroy(_tintMaterial);
                _tintMaterial = null;
            }

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }

            _filter = null;
            _meshRenderer = null;
            _spriteRenderer = null;
            _mode = ContentMode.None;
        }

        Quaternion ResolvePreviewRotation()
        {
            if (_tileSlot == TilePlacementSlot.HorizontalFace)
                return Quaternion.identity;

            if (_tileSlot == TilePlacementSlot.VerticalFace)
                return Quaternion.identity;

            return Quaternion.Euler(0f, (_facingQuarters & 3) * 90f, 0f);
        }

        void EnsureRoot(string name)
        {
            if (_root != null)
                return;

            GameObject prefab = Resources.Load<GameObject>(ConstructionConsts.TargetPreviewResourcesName);
            if (prefab != null)
            {
                _root = Object.Instantiate(prefab);
                _root.name = name;
            }
            else
            {
                Debug.LogWarning(
                    "[CellTargetPreview3D] Prefab missing at Resources/" +
                    ConstructionConsts.TargetPreviewResourcesName +
                    " — building Mesh/Sprite children at runtime.");
                _root = new GameObject(name);
                MapPlantVisualHierarchy.EnsureChildren(
                    _root.transform,
                    out _,
                    out _,
                    out _);
            }

            MapPlantVisualHierarchy.CacheFromRoot(
                _root.transform,
                out _filter,
                out _meshRenderer,
                out _spriteRenderer);

            if (_meshRenderer != null)
            {
                _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                _meshRenderer.receiveShadows = false;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
                _spriteRenderer.receiveShadows = false;
                _spriteRenderer.enabled = false;
            }

            _root.SetActive(false);
        }

        void ApplyPlantTint(Color tint)
        {
            if (_spriteRenderer != null &&
                _spriteRenderer.enabled &&
                _spriteRenderer.sprite != null)
            {
                _spriteRenderer.color = tint;
                return;
            }

            if (_meshRenderer == null)
                return;

            if (_tintMaterial == null)
            {
                Shader shader = Shader.Find(ConstructionConsts.OverlayShaderUrpUnlit);
                if (shader == null)
                    shader = Shader.Find(ConstructionConsts.OverlayShaderUnlitColor);
                if (shader == null)
                    return;

                _tintMaterial = new Material(shader) { name = "CellTargetPreview3D" };
                _meshRenderer.sharedMaterial = _tintMaterial;
            }

            if (_tintMaterial.HasProperty("_BaseColor"))
                _tintMaterial.SetColor("_BaseColor", tint);
            else if (_tintMaterial.HasProperty("_Color"))
                _tintMaterial.SetColor("_Color", tint);
        }

        static void ApplyTintToRenderers(GameObject root, Color tint)
        {
            if (root == null)
                return;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null)
                    continue;

                Material[] mats = r.materials;
                for (int m = 0; m < mats.Length; m++)
                {
                    Material mat = mats[m];
                    if (mat == null)
                        continue;
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", tint);
                    else if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", tint);
                }

                r.materials = mats;
            }

            SpriteRenderer[] sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                    sprites[i].color = tint;
            }
        }

        static void DisableCollidersAndScripts(GameObject root)
        {
            Collider[] cols = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null)
                    cols[i].enabled = false;
            }

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour b = behaviours[i];
                if (b != null)
                    b.enabled = false;
            }
        }

        void ClearTileGhost()
        {
            if (_tileGhostInstance == null)
                return;

            Object.Destroy(_tileGhostInstance);
            _tileGhostInstance = null;
            if (_filter != null)
                _filter.gameObject.SetActive(true);
        }
    }
}
