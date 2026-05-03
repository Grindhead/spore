Shader "RoobsWorld/WaterBackdrop"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.62, 0.89, 0.98, 0.36)
        _DeepColor("Deep Color", Color) = (0.06, 0.23, 0.35, 0.58)
        _FoamColor("Foam Color", Color) = (0.86, 0.98, 1.0, 0.38)
        _WaveScale("Wave Scale", Float) = 3.0
        _WorldTiling("World Tiling", Float) = 0.08
        _WaveSpeed("Wave Speed", Float) = 0.18
        _Distortion("Distortion", Range(0, 1)) = 0.22
        _CausticStrength("Caustic Strength", Range(0, 1)) = 0.32
        _EdgeFade("Edge Fade", Range(0.1, 4)) = 1.25
        _EdgeFadeStrength("Edge Fade Strength", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _DeepColor;
                half4 _FoamColor;
                float _WaveScale;
                float _WorldTiling;
                float _WaveSpeed;
                float _Distortion;
                float _CausticStrength;
                float _EdgeFade;
                float _EdgeFadeStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y * _WaveSpeed;
                float2 worldUv = input.positionWS.xz * _WorldTiling;
                float waveAFrequency = _WaveScale;
                float waveBFrequency = _WaveScale * 1.75;
                float causticFrequency = _WaveScale * 3.25;

                float2 flowA = float2(t * 0.7, -t * 0.45);
                float2 flowB = float2(-t * 0.35, t * 0.62);

                float noiseA = ValueNoise(worldUv * waveAFrequency + flowA);
                float noiseB = ValueNoise(worldUv * waveBFrequency + flowB);
                float wave = saturate((noiseA * 0.62 + noiseB * 0.38) * 1.15);

                float2 distort = float2(noiseB - 0.5, noiseA - 0.5) * _Distortion * 0.25;
                float caustics = ValueNoise((worldUv + distort) * causticFrequency + float2(t * 1.6, t * 1.15));
                caustics = smoothstep(0.68, 1.0, caustics) * _CausticStrength;

                // Keep optional edge fade in world space so tiled UVs do not reveal chunk boundaries.
                float2 centered = (input.positionWS.xz - _WorldSpaceCameraPos.xz) * (_WorldTiling * 0.75);
                float edge = saturate(1.0 - length(centered));
                edge = pow(edge, _EdgeFade);

                half3 col = lerp(_DeepColor.rgb, _BaseColor.rgb, wave);
                col = lerp(col, _FoamColor.rgb, caustics);

                half alpha = saturate(_BaseColor.a * (0.72 + wave * 0.38));
                alpha *= lerp(1.0, edge, _EdgeFadeStrength);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
