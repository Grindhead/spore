Shader "RoobsWorld/UI/MenuLivingBackdrop"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Tint("Tint", Color) = (1, 1, 1, 1)
        _DeepColor("Deep Color", Color) = (0.02, 0.07, 0.22, 1)
        _BloomColor("Bloom Color", Color) = (0.2, 0.95, 1, 1)
        _AccentColor("Accent Color", Color) = (1, 0.36, 0.72, 1)
        _FlowSpeed("Flow Speed", Float) = 0.18
        _Warp("Warp", Range(0, 1)) = 0.32
        _CausticStrength("Caustic Strength", Range(0, 2)) = 0.85
        _PlanktonStrength("Plankton Strength", Range(0, 2)) = 0.5
        _VignetteStrength("Vignette Strength", Range(0, 1)) = 0.38
        _Seed("Seed", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "LivingMenuBackdrop"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half4 _DeepColor;
                half4 _BloomColor;
                half4 _AccentColor;
                float _FlowSpeed;
                float _Warp;
                float _CausticStrength;
                float _PlanktonStrength;
                float _VignetteStrength;
                float _Seed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345) + _Seed);
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
                float2 uv = input.uv;
                float t = _Time.y * _FlowSpeed;

                float slowNoise = ValueNoise(uv * 3.2 + float2(t * 0.9, -t * 0.55));
                float fineNoise = ValueNoise(uv * 9.0 + float2(-t * 1.8, t * 1.35));
                float2 warp = float2(slowNoise - 0.5, fineNoise - 0.5) * _Warp * 0.08;

                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + warp) * _Tint * input.color;

                float ribbon = sin((uv.x * 0.72 + uv.y * 0.18 + t * 0.86) * 7.5 + slowNoise * 2.1);
                ribbon += sin((uv.x * 0.34 - uv.y * 0.12 - t * 0.62) * 4.2 + fineNoise * 1.7) * 0.55;
                ribbon = ribbon * 0.5 + 0.5;
                float softCurrent = smoothstep(0.42, 0.92, ribbon);
                float shimmer = smoothstep(0.58, 0.98, ValueNoise((uv + warp * 1.7) * 14.0 + float2(t * 2.1, t * 1.25)));
                float caustic = softCurrent * shimmer * (0.55 + slowNoise * 0.45) * _CausticStrength;

                float planktonMask = smoothstep(0.78, 0.98, ValueNoise(uv * 42.0 + float2(t * 7.0, -t * 4.5)));
                float planktonPulse = 0.62 + 0.38 * sin(_Time.y * 2.8 + uv.x * 12.0 + uv.y * 8.0);
                float plankton = planktonMask * planktonPulse * _PlanktonStrength;

                float2 centered = uv * 2.0 - 1.0;
                float vignette = saturate(1.0 - dot(centered, centered) * _VignetteStrength);

                half3 color = lerp(_DeepColor.rgb, source.rgb, source.a);
                color += _BloomColor.rgb * caustic * 0.32;
                color += lerp(_BloomColor.rgb, _AccentColor.rgb, fineNoise) * plankton * 0.55;
                color *= vignette;

                return half4(saturate(color), saturate(source.a * _Tint.a));
            }
            ENDHLSL
        }
    }
}
