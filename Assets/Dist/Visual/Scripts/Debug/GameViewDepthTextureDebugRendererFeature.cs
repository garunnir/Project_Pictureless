// ============================================================
// GameViewDepthTextureDebugRendererFeature — Game MainCamera depth 디버그 URP 패스
// ============================================================

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Play 중 F10으로 켠 뒤, <c>MainCamera</c> Game 뷰에 <c>_CameraDepthTexture</c> Linear01 그레이스케일을 그린다.
/// </summary>
public sealed class GameViewDepthTextureDebugRendererFeature : ScriptableRendererFeature
{
    const string DefaultShaderName = "Hidden/Dist/GameViewDepthTextureDebug";

    [SerializeField] Shader _shader;
    [SerializeField] RenderPassEvent _passEvent = RenderPassEvent.AfterRendering;

    Material _material;
    GameViewDepthTextureDebugPass _pass;

    public override void Create()
    {
        _pass = new GameViewDepthTextureDebugPass
        {
            renderPassEvent = _passEvent,
        };

        if (_shader == null)
            _shader = Shader.Find(DefaultShaderName);

        if (_shader == null)
        {
            Debug.LogWarning($"[{nameof(GameViewDepthTextureDebugRendererFeature)}] Shader not found: {DefaultShaderName}");
            return;
        }

        _material = CoreUtils.CreateEngineMaterial(_shader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_pass == null || _material == null)
            return;

        if (!GameViewDepthTextureDebug.Enabled)
            return;

        Camera camera = renderingData.cameraData.camera;
        if (camera == null)
            return;

        if (camera.cameraType != CameraType.Game)
            return;

        if (!camera.CompareTag("MainCamera"))
            return;

        UniversalAdditionalCameraData additionalData = camera.GetUniversalAdditionalCameraData();
        if (additionalData != null && additionalData.renderType == CameraRenderType.Overlay)
            return;

        _pass.Setup(_material);
        _pass.ConfigureInput(ScriptableRenderPassInput.Depth);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_material);
        _material = null;
        _pass = null;
    }
}
