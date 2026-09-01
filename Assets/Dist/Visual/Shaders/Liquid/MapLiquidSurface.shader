// ============================================================
// MapLiquidSurface — 그리드 액체 수면 셰이더 (월드 XZ 노이즈 · 픽셀 톤 · URP 씬 깊이 해안)
// ============================================================
// 정점 색: r=depth01, g=foam01, b=isTop
// Fill01·foam01은 메셔 SSOT. 씬 깊이(_CameraDepthTexture)는 바닥과의 교차(해안 폼) 보조.

Shader "Dist/MapLiquidSurface"
{
    Properties
    {
        [Header(Color)]
        _ShallowColor ("Shallow Color", Color) = (0.36, 0.78, 0.85, 0.62)
        _DeepColor ("Deep Color", Color) = (0.06, 0.24, 0.48, 0.88)
        _DepthPower ("Depth Falloff", Range(0.2, 4)) = 1.1
        _SideTint ("Side Face Tint", Range(0.3, 1)) = 0.78

        [Header(Waves)]
        _WaveScale ("Wave Scale", Float) = 0.55
        _WaveSpeed ("Wave Speed", Float) = 0.12
        _WaveStrength ("Wave Strength", Range(0, 0.6)) = 0.16
        _QuantizeSteps ("Color Quantize Steps", Range(2, 32)) = 6

        [Header(Glint)]
        _GlintColor ("Glint Color", Color) = (0.86, 0.97, 1, 1)
        _GlintThreshold ("Glint Threshold", Range(0.5, 1)) = 0.78
        _GlintStrength ("Glint Strength", Range(0, 1)) = 0.45

        [Header(Foam)]
        _FoamColor ("Foam Color", Color) = (0.93, 0.99, 1, 0.9)
        _FoamWidth ("Foam Width", Range(0, 1)) = 0.42
        _FoamSoftness ("Foam Edge Softness", Range(0, 0.5)) = 0.14
        _FoamNoiseScale ("Foam Noise Scale", Float) = 1.7
        _FoamNoiseStrength ("Foam Noise Strength", Range(0, 0.6)) = 0.22
        _FoamSpeed ("Foam Speed", Float) = 0.25

        [Header(Scene Depth Shore)]
        [Toggle] _UseSceneDepth ("Use Scene Depth", Float) = 1
        _ShoreDepthFade ("Shore Depth Fade (world Y)", Range(0.01, 2)) = 0.35
        _ShoreFoamStrength ("Shore Foam Strength", Range(0, 1)) = 0.7
        _ShoreDepthBias ("Shore Depth Bias (world Y)", Range(-0.5, 0.5)) = 0.02

        [Header(Lighting)]
        _LightInfluence ("Light Influence", Range(0, 1)) = 0.6
        _AmbientFloor ("Ambient Floor", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "LiquidSurfaceForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float _DepthPower;
                float _SideTint;
                float _WaveScale;
                float _WaveSpeed;
                float _WaveStrength;
                float _QuantizeSteps;
                float4 _GlintColor;
                float _GlintThreshold;
                float _GlintStrength;
                float4 _FoamColor;
                float _FoamWidth;
                float _FoamSoftness;
                float _FoamNoiseScale;
                float _FoamNoiseStrength;
                float _FoamSpeed;
                float _UseSceneDepth;
                float _ShoreDepthFade;
                float _ShoreFoamStrength;
                float _ShoreDepthBias;
                float _LightInfluence;
                float _AmbientFloor;
            CBUFFER_END

            float _MapLiquidTime;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogCoord : TEXCOORD2;
                float4 color : COLOR;
            };

            float2 Mod289(float2 p)
            {
                return p - 289.0 * floor(p * (1.0 / 289.0));
            }

            float Mod289Scalar(float v)
            {
                return v - 289.0 * floor(v * (1.0 / 289.0));
            }

            float2 GradientDir(float2 p)
            {
                p = Mod289(p);
                float x = Mod289Scalar((34.0 * p.x + 1.0) * p.x) + p.y;
                x = Mod289Scalar((34.0 * x + 1.0) * x);
                x = frac(x / 41.0) * 2.0 - 1.0;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }

            float GradientNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f);
                float n00 = dot(GradientDir(i + float2(0, 0)), f - float2(0, 0));
                float n10 = dot(GradientDir(i + float2(1, 0)), f - float2(1, 0));
                float n01 = dot(GradientDir(i + float2(0, 1)), f - float2(0, 1));
                float n11 = dot(GradientDir(i + float2(1, 1)), f - float2(1, 1));
                return lerp(lerp(n00, n10, u.x), lerp(n01, n11, u.x), u.y) + 0.5;
            }

            float Quantize(float v, float steps)
            {
                steps = max(steps, 1.0);
                return floor(saturate(v) * steps) / steps;
            }

            // eye-depth 차는 카메라 이동·직교에 따라 잔상이 밀린다. 같은 픽셀 깊이→월드 Y만 비교.
            float SampleShoreDepthFoam(float4 positionCS, float3 positionWS, float isTop)
            {
                if (_UseSceneDepth < 0.5 || isTop < 0.5)
                    return 0.0;

                uint2 pixelCoord = uint2(positionCS.xy);
                float sceneRaw = LoadSceneDepth(pixelCoord);
                float2 screenUV = GetNormalizedScreenSpaceUV(float4(pixelCoord + 0.5, positionCS.zw));
                float3 sceneWS = ComputeWorldSpacePosition(screenUV, sceneRaw, UNITY_MATRIX_I_VP);

                float deltaY = positionWS.y - sceneWS.y + _ShoreDepthBias;
                if (deltaY <= 0.0)
                    return 0.0;

                return 1.0 - saturate(deltaY / max(_ShoreDepthFade, 1e-4));
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogCoord = ComputeFogFactor(pos.positionCS.z);
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float depth01 = input.color.r;
                float foam01 = input.color.g;
                float isTop = input.color.b;

                float2 wp = input.positionWS.xz;
                float t = _MapLiquidTime;

                float wave = GradientNoise(wp * _WaveScale + float2(t * _WaveSpeed, t * _WaveSpeed * 0.73));
                wave += 0.5 * GradientNoise(wp * _WaveScale * 2.13 - float2(t * _WaveSpeed * 0.61, t * _WaveSpeed * 0.94));
                wave /= 1.5;

                float depthShade = saturate(pow(saturate(depth01), _DepthPower));
                float3 baseRgb = lerp(_ShallowColor.rgb, _DeepColor.rgb, Quantize(depthShade, _QuantizeSteps));
                float baseAlpha = lerp(_ShallowColor.a, _DeepColor.a, depthShade);

                float waveStep = Quantize(wave, _QuantizeSteps) - 0.5;
                baseRgb += waveStep * _WaveStrength;

                float glint = step(_GlintThreshold, wave) * isTop;
                baseRgb = lerp(baseRgb, _GlintColor.rgb, glint * _GlintStrength);

                float foamNoise = GradientNoise(wp * _FoamNoiseScale + float2(t * _FoamSpeed, -t * _FoamSpeed * 0.5));
                float foamEdge = foam01 + (foamNoise - 0.5) * _FoamNoiseStrength;
                float foamLow = 1.0 - _FoamWidth - _FoamSoftness;
                float foamHigh = 1.0 - _FoamWidth + _FoamSoftness;
                float meshFoam = smoothstep(foamLow, foamHigh, foamEdge);

                float shoreFoam = SampleShoreDepthFoam(input.positionCS, input.positionWS, isTop) * _ShoreFoamStrength;
                float foamMask = saturate(max(meshFoam, shoreFoam));

                float3 rgb = lerp(baseRgb, _FoamColor.rgb, foamMask);
                float alpha = lerp(baseAlpha, _FoamColor.a, foamMask);

                rgb *= lerp(_SideTint, 1.0, isTop);

                Light mainLight = GetMainLight();
                float3 ambient = SampleSH(input.normalWS);
                float ndotl = saturate(dot(normalize(input.normalWS), mainLight.direction));
                float3 lit = ambient + mainLight.color * mainLight.distanceAttenuation * ndotl;
                lit = max(lit, float3(_AmbientFloor, _AmbientFloor, _AmbientFloor));
                rgb *= lerp(float3(1.0, 1.0, 1.0), lit, _LightInfluence);

                rgb = MixFog(rgb, input.fogCoord);
                return half4(rgb, saturate(alpha));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
