// ============================================================
// MapBloodStainRenderer — 혈흔 스탬프를 DrawMeshInstanced로 배치 그림
// ============================================================

using UnityEngine;

namespace IsoTilemap
{
    [DisallowMultipleComponent]
    public sealed class MapBloodStainRenderer : MonoBehaviour
    {
        [SerializeField] Mesh _quadMesh;
        [SerializeField] Material _stainMaterial;
        [SerializeField] int _layer;

        MapBloodOverlay _overlay;
        Matrix4x4[] _matrices = new Matrix4x4[MapBloodConsts.DrawBatchSize];

        public void Bind(MapBloodOverlay overlay)
        {
            _overlay = overlay;
        }

        void LateUpdate()
        {
            if (_overlay == null)
                return;

            Material mat = _stainMaterial != null ? _stainMaterial : EnsureFallbackMaterial();
            if (mat == null)
                return;

            int count = _overlay.Count;
            if (count <= 0)
                return;

            Mesh mesh = _quadMesh != null ? _quadMesh : EnsureUnitQuad();
            if (mesh == null)
                return;

            int batch = MapBloodConsts.DrawBatchSize;
            int offset = 0;
            while (offset < count)
            {
                int n = Mathf.Min(batch, count - offset);
                for (int i = 0; i < n; i++)
                {
                    BloodStamp s = _overlay.Stamps[offset + i];
                    Vector3 pos = s.WorldPos;
                    pos.y += MapBloodConsts.StainYOffset;
                    Quaternion rot = Quaternion.Euler(90f, s.Yaw, 0f);
                    Vector3 scale = new Vector3(s.Scale, s.Scale, 1f);
                    _matrices[i] = Matrix4x4.TRS(pos, rot, scale);
                }

                Graphics.DrawMeshInstanced(
                    mesh,
                    0,
                    mat,
                    _matrices,
                    n,
                    null,
                    UnityEngine.Rendering.ShadowCastingMode.Off,
                    false,
                    _layer);
                offset += n;
            }
        }

        Material EnsureFallbackMaterial()
        {
            if (_stainMaterial != null)
                return _stainMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                return null;

            _stainMaterial = new Material(shader) { name = "MapBloodFallback", enableInstancing = true };
            if (_stainMaterial.HasProperty("_BaseColor"))
                _stainMaterial.SetColor("_BaseColor", new Color(0.45f, 0.02f, 0.02f, 0.75f));
            else if (_stainMaterial.HasProperty("_Color"))
                _stainMaterial.SetColor("_Color", new Color(0.45f, 0.02f, 0.02f, 0.75f));
            return _stainMaterial;
        }

        Mesh EnsureUnitQuad()
        {
            if (_quadMesh != null)
                return _quadMesh;

            var mesh = new Mesh { name = "MapBloodUnitQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _quadMesh = mesh;
            return mesh;
        }
    }
}
