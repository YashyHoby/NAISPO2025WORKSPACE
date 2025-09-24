Shader "Unlit/AgentSDF"
{
    Properties
    {
        _Tint("Tint", Color) = (1,1,1,1)

        // HeartVisual から供給されるパラメータ
        _Pulse("Pulse", Float) = 0.0
        _Radius("Radius", Float) = 0.6
        _ShapeType("ShapeType", Float) = 0.0
        _IsCircle("IsCircle", Float) = 1.0

        // ゼリー質感
        _Smoothness("Smoothness", Range(0,1)) = 0.1
        _SpecularColor("SpecularColor", Color) = (1,1,1,0.5)
        _Shininess("Shininess", Range(1,128)) = 20.0
        _FresnelColor("FresnelColor", Color) = (1,1,1,0.1)
        _FresnelPower("FresnelPower", Range(0.1,10)) = 2.0
        _Refraction("Refraction", Range(0,1)) = 0.1

        // アウトライン（将来拡張用）
        _OutlineWidth("OutlineWidth", Range(0,0.1)) = 0.0
        _OutlineColor("OutlineColor", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardUnlitJelly"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SURFACE_TYPE_TRANSPARENT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
            };

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            float4 _Tint;

            // HeartVisual parameters
            float _Pulse;
            float _Radius;
            float _ShapeType;
            float _IsCircle;

            // Jelly params
            float  _Smoothness;
            float4 _SpecularColor;
            float  _Shininess;
            float4 _FresnelColor;
            float  _FresnelPower;
            float  _Refraction;

            float  _OutlineWidth;
            float4 _OutlineColor;

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = input.uv;
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            // 疑似法線（ゼリーのうねり
            float3 ComputeFakeNormal(float2 uv, float pulse)
            {
                float2 p = uv * 2.0 - 1.0;
                float ripple = 0.04 * sin(6.28318 * (p.x + p.y + pulse));
                float3 n = normalize(float3(p.x + ripple, p.y - ripple, 1.5));
                return n;
            }

            float3 ComputeSpecular(float3 N, float3 V, float3 L, float shininess, float specStr)
            {
                float3 H = normalize(L + V);
                float NdotH = saturate(dot(N, H));
                float spec = pow(NdotH, max(1.0, shininess)) * specStr;
                return spec.xxx;
            }

            float4 frag(Varyings i) : SV_Target
            {
                // 2D用の簡易ライティング
                float3 N = ComputeFakeNormal(i.uv, _Pulse);
                float3 V = normalize(float3(0,0,1));
                float3 L = normalize(float3(0.4, 0.6, 1.0));

                float3 baseCol = _Tint.rgb;

                // Fresnel rim
                float NdotV = saturate(dot(N, V));
                float fres  = pow(1.0 - NdotV, max(0.1, _FresnelPower));
                float3 fresCol = _FresnelColor.rgb * fres * _FresnelColor.a;

                // Specular
                float specStrength = lerp(0.05, 1.0, saturate(_Smoothness));
                float3 specCol = ComputeSpecular(N, V, L, _Shininess, specStrength) * _SpecularColor.rgb * _SpecularColor.a;

                // Pseudo refraction
                float2 uvSS = i.screenPos.xy / i.screenPos.w;
                float2 screenUV = UnityStereoTransformScreenSpaceTex(uvSS);
                float2 offset = (N.xy) * (_Refraction * lerp(0.4, 1.0, _Pulse));
                float3 sceneCol = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + offset).rgb;

                // Composite
                float baseWeight = 0.6;
                float3 color = lerp(sceneCol, baseCol, baseWeight);
                color += fresCol;
                color += specCol;

                float alpha = _Tint.a;
                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
}
