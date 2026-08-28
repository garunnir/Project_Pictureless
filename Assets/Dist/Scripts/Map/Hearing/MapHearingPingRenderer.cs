// ============================================================
// MapHearingPingRenderer — 셀 중심 floor quad + radial alpha
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    [DisallowMultipleComponent]
    public sealed class MapHearingPingRenderer : MonoBehaviour
    {
        [SerializeField] Mesh _quadMesh;
        [SerializeField] Material _pingMaterial;
        [SerializeField] int _layer;

        MapHearingPingOverlay _overlay;
        float _cellSize = 1f;
        float _quadSize = 0.95f;
        float _yOffset = 0.02f;
        float _maxAlpha = 0.55f;
        Material _drawMaterial;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color PingRgb = new(0.45f, 0.75f, 1f, 1f);

        void Awake()
        {
            if (_layer == 0)
                _layer = gameObject.layer;
            ResolveDrawMaterial();
        }

        void OnDestroy() => DestroyDrawMaterial();

        public void Bind(MapHearingPingOverlay overlay, float cellSize)
        {
            _overlay = overlay;
            _cellSize = Mathf.Max(1e-4f, cellSize);
        }

        public void ConfigureDraw(float quadSizeMeters, float yOffsetMeters, float maxAlpha)
        {
            _quadSize = Mathf.Max(0.1f, quadSizeMeters);
            _yOffset = Mathf.Max(0f, yOffsetMeters);
            _maxAlpha = Mathf.Clamp01(maxAlpha);
        }

        void LateUpdate()
        {
            if (_overlay == null || _overlay.Count <= 0)
                return;

            Material mat = ResolveDrawMaterial();
            if (mat == null)
                return;

            Mesh mesh = _quadMesh != null ? _quadMesh : EnsureUnitQuad();
            if (mesh == null)
                return;

            IReadOnlyList<HearingPingEntry> entries = _overlay.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                HearingPingEntry entry = entries[i];
                Vector3 pos = entry.WorldPos;
                pos.y += _yOffset;
                float scale = _quadSize;
                var matrix = Matrix4x4.TRS(pos, Quaternion.Euler(90f, 0f, 0f), new Vector3(scale, scale, 1f));
                float alpha = entry.Alpha * _maxAlpha;
                mat.SetColor(BaseColorId, new Color(PingRgb.r, PingRgb.g, PingRgb.b, alpha));
                Graphics.DrawMesh(
                    mesh,
                    matrix,
                    mat,
                    _layer,
                    null,
                    0,
                    null,
                    false,
                    false,
                    false);
            }
        }

        Material ResolveDrawMaterial()
        {
            if (_drawMaterial != null)
                return _drawMaterial;

            if (_pingMaterial != null)
            {
                _drawMaterial = new Material(_pingMaterial) { name = $"{_pingMaterial.name} (PingDraw)" };
                return _drawMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Transparent");
            if (shader == null)
                return null;

            _drawMaterial = new Material(shader) { name = "MapHearingPingFallback" };
            if (_drawMaterial.HasProperty(BaseColorId))
                _drawMaterial.SetColor(BaseColorId, new Color(PingRgb.r, PingRgb.g, PingRgb.b, 0.55f));

            Texture2D radial = EnsureRadialAlphaTexture();
            if (radial != null && _drawMaterial.HasProperty("_BaseMap"))
                _drawMaterial.SetTexture("_BaseMap", radial);

            ConfigureTransparent(_drawMaterial);
            return _drawMaterial;
        }

        void DestroyDrawMaterial()
        {
            if (_drawMaterial == null)
                return;

            if (Application.isPlaying)
                Destroy(_drawMaterial);
            else
                DestroyImmediate(_drawMaterial);

            _drawMaterial = null;
        }

        static void ConfigureTransparent(Material mat)
        {
            if (mat == null)
                return;

            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        static Texture2D s_radialTexture;

        static Texture2D EnsureRadialAlphaTexture()
        {
            if (s_radialTexture != null)
                return s_radialTexture;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "MapHearingPingRadial",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - dist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            s_radialTexture = tex;
            return s_radialTexture;
        }

        Mesh EnsureUnitQuad()
        {
            if (_quadMesh != null)
                return _quadMesh;

            var mesh = new Mesh { name = "MapHearingPingUnitQuad" };
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
