// ============================================================
// GameViewDepthTextureDebugPass — _CameraDepthTexture Linear01 그레이스케일 합성
// ============================================================

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Play 중 <see cref="GameViewDepthTextureDebug.Enabled"/>일 때 카메라 컬러를 depth 그레이스케일로 덮는다.
/// </summary>
public sealed class GameViewDepthTextureDebugPass : ScriptableRenderPass
{
    static readonly int TempColorId = Shader.PropertyToID("_GameViewDepthDebugTemp");
    static readonly Vector4 ScaleBias = new(1f, 1f, 0f, 0f);
    static readonly ProfilingSampler ProfilerSampler = new(nameof(GameViewDepthTextureDebugPass));

    Material _material;

    public GameViewDepthTextureDebugPass()
    {
        profilingSampler = ProfilerSampler;
    }

    public void Setup(Material material) => _material = material;

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (_material == null)
            return;

        ScriptableRenderer renderer = renderingData.cameraData.renderer;
        RTHandle source = renderer.cameraColorTargetHandle;
        if (source == null || source.rt == null)
            return;

        RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1;

        CommandBuffer cmd = CommandBufferPool.Get(nameof(GameViewDepthTextureDebugPass));
        cmd.GetTemporaryRT(TempColorId, desc, FilterMode.Bilinear);
        cmd.SetRenderTarget(TempColorId);
        Blitter.BlitTexture(cmd, source, ScaleBias, _material, 0);
        cmd.Blit(TempColorId, source);
        cmd.ReleaseTemporaryRT(TempColorId);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
