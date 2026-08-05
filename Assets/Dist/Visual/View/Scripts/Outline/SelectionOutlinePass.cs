using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// ============================================================
// SelectionOutlinePass
// URP 외곽선 합성 패스 (RenderGraph + Compatibility Mode Execute).
// 1) Mask Pass : RenderingLayer 매칭 렌더러를 overrideShader로 R8에 알파 실루엣 기록
// 2) Composite Pass : 카메라 컬러 + 마스크를 읽어 임시 컬러 RT에 외곽선 합성
// 3) CopyBack Pass  : 임시 컬러 RT를 카메라 컬러로 복사
//
// Hot path: 마스크는 선택 오브젝트 프래그만(+텍스처 1샘플·clip). 풀스크린 합성은 기존과 동일.
// overrideShader는 해당 Draw에서 SRP Batcher 미사용(선택 소수 전제).
// Compatibility Mode(URP_COMPATIBILITY_MODE / ProPixelizer)에서는 Execute 경로 사용.
// ============================================================
public class SelectionOutlinePass : ScriptableRenderPass
{
    private const string k_MaskPassName = "Selection.Mask";
    private const string k_CompositePassName = "Selection.OutlineComposite";
    private const string k_CopyBackPassName = "Selection.OutlineCopyBack";
    private const string k_CompatProfilerTag = "SelectionOutline.Compat";
    private const string k_MaskTextureName = "_SelectionMaskTex";
    private const string k_TempColorTextureName = "_SelectionTempColorTex";

    private static readonly int s_MaskTexId = Shader.PropertyToID("_MaskTex");
    private static readonly int s_OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int s_ThicknessPxId = Shader.PropertyToID("_ThicknessPx");
    private static readonly int s_MaskTempRtId = Shader.PropertyToID("_SelectionMaskTex");
    private static readonly int s_TempColorRtId = Shader.PropertyToID("_SelectionTempColorTex");

    private static readonly Vector4 s_ScaleBias = new Vector4(1f, 1f, 0f, 0f);
    private static readonly ShaderTagId[] s_DefaultShaderTagIds =
    {
        new ShaderTagId("UniversalForward"),
        new ShaderTagId("UniversalForwardOnly"),
        new ShaderTagId("SRPDefaultUnlit"),
    };

    private readonly Shader _maskShader;
    private readonly Material _outlineMaterial;
    private readonly uint _renderingLayerMask;
    private readonly Color _outlineColor;
    private readonly int _thicknessPx;

    public SelectionOutlinePass(Shader maskShader, Material outlineMaterial, uint renderingLayerMask, Color outlineColor, int thicknessPx)
    {
        _maskShader = maskShader;
        _outlineMaterial = outlineMaterial;
        _renderingLayerMask = renderingLayerMask;
        _outlineColor = outlineColor;
        _thicknessPx = Mathf.Max(1, thicknessPx);
    }

    private class MaskPassData
    {
        public RendererListHandle rendererList;
    }

    private class CompositePassData
    {
        public Material material;
        public TextureHandle source;
        public TextureHandle mask;
        public Color outlineColor;
        public int thicknessPx;
    }

    private class CopyPassData
    {
        public TextureHandle source;
    }

    /// <summary>
    /// Compatibility Mode (Render Graph disabled) path used with ProPixelizer on Unity 6.3+.
    /// </summary>
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (_maskShader == null || _outlineMaterial == null)
            return;
        if (_renderingLayerMask == 0u)
            return;

        ScriptableRenderer renderer = renderingData.cameraData.renderer;
        RTHandle cameraColor = renderer.cameraColorTargetHandle;
        RTHandle cameraDepth = renderer.cameraDepthTargetHandle;
        if (cameraColor == null || !cameraColor.rt)
            return;

        CommandBuffer cmd = CommandBufferPool.Get(k_CompatProfilerTag);
        RenderTextureDescriptor camDesc = renderingData.cameraData.cameraTargetDescriptor;
        int width = Mathf.Max(1, camDesc.width);
        int height = Mathf.Max(1, camDesc.height);

        var maskDesc = new RenderTextureDescriptor(width, height, GraphicsFormat.R8_UNorm, GraphicsFormat.None, 0)
        {
            msaaSamples = 1,
            useMipMap = false,
            autoGenerateMips = false,
        };
        var tempColorDesc = camDesc;
        tempColorDesc.depthBufferBits = 0;
        tempColorDesc.msaaSamples = 1;

        cmd.GetTemporaryRT(s_MaskTempRtId, maskDesc, FilterMode.Point);
        cmd.GetTemporaryRT(s_TempColorRtId, tempColorDesc, FilterMode.Bilinear);

        if (cameraDepth != null && cameraDepth.rt)
            cmd.SetRenderTarget(s_MaskTempRtId, cameraDepth);
        else
            cmd.SetRenderTarget(s_MaskTempRtId);
        cmd.ClearRenderTarget(false, true, Color.clear);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        SortingCriteria sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
        FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all, ~0, _renderingLayerMask);
        DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(
            s_DefaultShaderTagIds[0], ref renderingData, sortFlags);
        for (int i = 1; i < s_DefaultShaderTagIds.Length; i++)
            drawSettings.SetShaderPassName(i, s_DefaultShaderTagIds[i]);
        drawSettings.overrideShader = _maskShader;
        drawSettings.overrideShaderPassIndex = 0;
        context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);

        _outlineMaterial.SetColor(s_OutlineColorId, _outlineColor);
        _outlineMaterial.SetFloat(s_ThicknessPxId, _thicknessPx);
        cmd.SetGlobalTexture(s_MaskTexId, s_MaskTempRtId);
        cmd.SetRenderTarget(s_TempColorRtId);
        Blitter.BlitTexture(cmd, cameraColor, s_ScaleBias, _outlineMaterial, 0);

        cmd.Blit(s_TempColorRtId, cameraColor);

        cmd.ReleaseTemporaryRT(s_MaskTempRtId);
        cmd.ReleaseTemporaryRT(s_TempColorRtId);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (_maskShader == null || _outlineMaterial == null) return;
        if (_renderingLayerMask == 0u) return;

        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        if (resourceData.isActiveTargetBackBuffer) return;

        TextureHandle cameraColor = resourceData.activeColorTexture;
        TextureHandle cameraDepth = resourceData.activeDepthTexture;
        if (!cameraColor.IsValid()) return;

        var camDesc = cameraData.cameraTargetDescriptor;
        int width = Mathf.Max(1, camDesc.width);
        int height = Mathf.Max(1, camDesc.height);

        var maskDesc = new RenderTextureDescriptor(width, height, GraphicsFormat.R8_UNorm, GraphicsFormat.None, 0)
        {
            msaaSamples = 1,
            useMipMap = false,
            autoGenerateMips = false,
        };
        TextureHandle maskHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, maskDesc, k_MaskTextureName, true);

        var tempColorDesc = camDesc;
        tempColorDesc.depthBufferBits = 0;
        tempColorDesc.msaaSamples = 1;
        TextureHandle tempColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, tempColorDesc, k_TempColorTextureName, true);

        // ---- Pass 1: Mask ----
        using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>(k_MaskPassName, out var passData))
        {
            SortingCriteria sortFlags = cameraData.defaultOpaqueSortFlags;
            RenderQueueRange queueRange = RenderQueueRange.all;
            FilteringSettings filterSettings = new FilteringSettings(queueRange, ~0, _renderingLayerMask);

            DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(
                s_DefaultShaderTagIds[0], renderingData, cameraData, lightData, sortFlags);
            for (int i = 1; i < s_DefaultShaderTagIds.Length; i++)
            {
                drawSettings.SetShaderPassName(i, s_DefaultShaderTagIds[i]);
            }
            // overrideMaterial은 원본 _MainTex/UV를 잃음 → overrideShader로 프로퍼티 유지 + 알파 clip
            drawSettings.overrideShader = _maskShader;
            drawSettings.overrideShaderPassIndex = 0;

            var listParams = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
            passData.rendererList = renderGraph.CreateRendererList(listParams);

            builder.UseRendererList(passData.rendererList);
            builder.SetRenderAttachment(maskHandle, 0, AccessFlags.Write);
            if (cameraDepth.IsValid())
            {
                builder.SetRenderAttachmentDepth(cameraDepth, AccessFlags.Read);
            }
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (MaskPassData data, RasterGraphContext ctx) =>
            {
                ctx.cmd.ClearRenderTarget(false, true, Color.clear);
                ctx.cmd.DrawRendererList(data.rendererList);
            });
        }

        // ---- Pass 2: Composite (camera color + mask -> tempColor) ----
        using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>(k_CompositePassName, out var passData))
        {
            passData.material = _outlineMaterial;
            passData.source = cameraColor;
            passData.mask = maskHandle;
            passData.outlineColor = _outlineColor;
            passData.thicknessPx = _thicknessPx;

            builder.UseTexture(cameraColor, AccessFlags.Read);
            builder.UseTexture(maskHandle, AccessFlags.Read);
            builder.SetRenderAttachment(tempColor, 0, AccessFlags.Write);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext ctx) =>
            {
                data.material.SetColor(s_OutlineColorId, data.outlineColor);
                data.material.SetFloat(s_ThicknessPxId, data.thicknessPx);
                data.material.SetTexture(s_MaskTexId, data.mask);
                Blitter.BlitTexture(ctx.cmd, data.source, s_ScaleBias, data.material, 0);
            });
        }

        // ---- Pass 3: Copy back to camera color ----
        using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>(k_CopyBackPassName, out var passData))
        {
            passData.source = tempColor;

            builder.UseTexture(tempColor, AccessFlags.Read);
            builder.SetRenderAttachment(cameraColor, 0, AccessFlags.Write);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (CopyPassData data, RasterGraphContext ctx) =>
            {
                Blitter.BlitTexture(ctx.cmd, data.source, s_ScaleBias, 0, false);
            });
        }
    }
}
