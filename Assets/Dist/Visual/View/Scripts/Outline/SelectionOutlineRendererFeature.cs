using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// ============================================================
// SelectionOutlineRendererFeature
// URP RendererFeature: SelectionLayerConfig가 가리키는 RenderingLayer 비트로 표시된
// 렌더러를 화면공간 외곽선으로 그린다. 머티리얼 스왑 없이 비트 토글만 사용.
// 마스크 Draw는 overrideShader(원본 프로퍼티 유지·알파 실루엣).
// ============================================================
public class SelectionOutlineRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private SelectionLayerConfig _layerConfig;
    [SerializeField] private Shader _maskShader;
    [SerializeField] private Shader _outlineShader;
    [SerializeField] private Color _outlineColor = Color.yellow;
    [SerializeField, Range(1, 8)] private int _outlineThicknessPx = 2;
    [SerializeField] private RenderPassEvent _passEvent = RenderPassEvent.AfterRenderingTransparents;

    private Material _outlineMaterial;
    private SelectionOutlinePass _pass;

    public override void Create()
    {
        DisposeMaterials();

        if (_maskShader == null || _outlineShader == null || _layerConfig == null)
        {
            return;
        }

        // 마스크는 overrideShader로 원본 머티리얼 프로퍼티(_MainTex, UV 워프, _Cutoff)를 유지한다.
        _outlineMaterial = CoreUtils.CreateEngineMaterial(_outlineShader);

        _pass = new SelectionOutlinePass(
            _maskShader,
            _outlineMaterial,
            _layerConfig.RenderingLayerMask,
            _outlineColor,
            _outlineThicknessPx)
        {
            renderPassEvent = _passEvent,
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_pass == null) return;
        if (renderingData.cameraData.cameraType != CameraType.Game)
        {
            return;
        }
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        DisposeMaterials();
        _pass = null;
    }

    private void DisposeMaterials()
    {
        if (_outlineMaterial != null)
        {
            CoreUtils.Destroy(_outlineMaterial);
            _outlineMaterial = null;
        }
    }
}
