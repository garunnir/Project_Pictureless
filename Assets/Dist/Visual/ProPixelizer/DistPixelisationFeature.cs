// Copyright Elliot Bentine, 2018-
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace Dist.ProPixelizer
{
    // ============================================================
    // DistPixelisationFeature — Dist fork of ProPixelizer PixelisationFeature.
    // Vendor Assets/ProPixelizer remains unmodified; URP Renderer should use this Feature.
    //
    // Runtime path: currently requires Player define URP_COMPATIBILITY_MODE so the
    // proven Execute() path runs. RecordRenderGraph exists but is incomplete on
    // Unity 6.3 (pixel-size holes / depth blit parity) — do not rely on it yet.
    //
    // REMOVE WHEN: ProPixelizer official v2 (native RecordRenderGraph) replaces
    // Assets/ProPixelizer, URP Renderer Feature is switched back to vendor
    // PixelisationFeature, then delete Assets/Dist/Visual/ProPixelizer/ entirely.
    // Also remove URP_COMPATIBILITY_MODE if nothing else needs it.
    // ============================================================
    public class DistPixelisationFeature : ScriptableRendererFeature
    {
        [FormerlySerializedAs("DepthTestOutlines")]
        [Tooltip("Perform depth testing for outlines where object IDs differ. This prevents outlines appearing when one object intersects another, but requires an extra depth sample.")]
        public bool UseDepthTestingForIDOutlines = true;

        [Tooltip("The threshold value used when depth comparing outlines.")]
        public float DepthTestThreshold = 0.001f;

        [Tooltip("Use normals for edge detection. This will analyse pixelated screen normals to determine where edges occur within an objects silhouette.")]
        public bool UseNormalsForEdgeDetection = true;

        public float NormalEdgeDetectionSensitivity = 1f;

        [Tooltip("Generates warnings if the pipeline state is incompatible with ProPixelizer.")]
        public bool GenerateWarnings = true;

        [HideInInspector, SerializeField]
        PixelizationPass.ShaderResources PixelizationShaders;
        [HideInInspector, SerializeField]
        OutlineDetectionPass.ShaderResources OutlineShaders;

        PixelizationPass _PixelisationPass;
        OutlineDetectionPass _OutlinePass;

        public override void Create()
        {
            PixelizationShaders = new PixelizationPass.ShaderResources().Load();
            OutlineShaders = new OutlineDetectionPass.ShaderResources().Load();
            _OutlinePass = new OutlineDetectionPass(OutlineShaders);
            _OutlinePass.DepthTestOutlines = UseDepthTestingForIDOutlines;
            _OutlinePass.DepthTestThreshold = DepthTestThreshold;
            _OutlinePass.UseNormalsForEdgeDetection = UseNormalsForEdgeDetection;
            _OutlinePass.NormalEdgeDetectionSensitivity = NormalEdgeDetectionSensitivity;
            _PixelisationPass = new PixelizationPass(PixelizationShaders, _OutlinePass);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            _PixelisationPass.ConfigureInput(ScriptableRenderPassInput.Color);
            #if UNITY_2022_1_OR_NEWER
            #else
            _PixelisationPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            #endif
            renderer.EnqueuePass(_OutlinePass);
            renderer.EnqueuePass(_PixelisationPass);

            if (GenerateWarnings)
                global::ProPixelizer.ProPixelizerVerification.GenerateWarnings();
        }

#if BLIT_API
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        _PixelisationPass.ConfigureInput(ScriptableRenderPassInput.Color);
    }
#endif

#if URP_13
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _OutlinePass.Dispose();
            _PixelisationPass.Dispose();
        }
    }
#endif
    }
}