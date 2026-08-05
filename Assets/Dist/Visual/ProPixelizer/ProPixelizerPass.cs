// Copyright Elliot Bentine, 2018-
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Dist.ProPixelizer
{
    /// <summary>
    /// Per-frame TextureHandles shared between OutlineDetectionPass and PixelizationPass (Render Graph).
    /// </summary>
    public sealed class ProPixelizerFrameData : ContextItem
    {
        public TextureHandle OutlineObjectColor;
        public TextureHandle OutlineObjectDepth;
        public TextureHandle OutlineBuffer;

        public override void Reset()
        {
            OutlineObjectColor = TextureHandle.nullHandle;
            OutlineObjectDepth = TextureHandle.nullHandle;
            OutlineBuffer = TextureHandle.nullHandle;
        }
    }

    /// <summary>
    /// Abstract base for ProPixelizer passes
    /// </summary>
    public abstract class ProPixelizerPass : ScriptableRenderPass
    {
        public static readonly int ProPixelizerRenderTargetInfoId = Shader.PropertyToID("_ProPixelizer_RenderTargetInfo");
        public static readonly Vector4 ScaleBiasIdentity = new Vector4(1f, 1f, 0f, 0f);

        public void Prepare(CommandBuffer buffer, ref RenderingData renderingData)
        {
            // Store the render target's size in a global variable.
            // This is required because Unity's RenderScale is not reliable, see
            // e.g. https://forum.unity.com/threads/_scaledscreenparameters-and-render-target-subregion.1336277/
            // 
            // The _ProPixelizer_RenderTargetSize variable is defined in the following way:
            // * x: target render region width
            // * y: target render region height
            // * z: fraction of target render region width over render target width.
            // * w: fraction of target render region height over render target height.
            //
            // The last two elements enable us to support functionality introduced in URP13, where
            // only a sub-region of the full render target is rendered to when renderscale != 1.

            //if (renderingData.cameraData.cameraType != CameraType.Preview)
            //{
#if URP_13
            var handleProps = RTHandles.rtHandleProperties;
            buffer.SetGlobalVector(ProPixelizerRenderTargetInfoId, new Vector4(renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height, handleProps.rtHandleScale.x, handleProps.rtHandleScale.y));
#else
            buffer.SetGlobalVector(ProPixelizerRenderTargetInfoId, new Vector4(renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height, 1.0f, 1.0f));
#endif
            //}
        }

        protected static Vector4 GetRenderTargetInfo(UniversalCameraData cameraData)
        {
#if URP_13
            var handleProps = RTHandles.rtHandleProperties;
            return new Vector4(
                cameraData.cameraTargetDescriptor.width,
                cameraData.cameraTargetDescriptor.height,
                handleProps.rtHandleScale.x,
                handleProps.rtHandleScale.y);
#else
            return new Vector4(
                cameraData.cameraTargetDescriptor.width,
                cameraData.cameraTargetDescriptor.height,
                1.0f,
                1.0f);
#endif
        }
    }
}