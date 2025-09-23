Shader "Unlit/JellyHeart"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Roundness ("Roundness", Range(0, 0.5)) = 0.2
        _Softness ("Softness", Range(0.001, 0.5)) = 0.05
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _Shininess ("Shininess", Range(1, 100)) = 20.0
        _FresnelColor ("Fresnel Color", Color) = (1,1,1,1)
        _FresnelPower ("Fresnel Power", Range(0.1, 10.0)) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Roundness;
            float _Softness;
            float4 _SpecularColor;
            float _Shininess;
            float4 _FresnelColor;
            float _FresnelPower;
            CBUFFER_END

            // SDF for a rounded box
            float sdRoundedBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // --- SDF Shape ---
                float2 p = i.uv - 0.5; // Center the coordinates
                float2 boxSize = float2(0.5 - _Roundness, 0.5 - _Roundness);
                float dist = sdRoundedBox(p, boxSize, _Roundness);

                // --- Alpha from SDF ---
                float alpha = 1.0 - smoothstep(0.0, _Softness, dist);
                if (alpha <= 0.0) discard;

                // --- Lighting ---
                float3 normal = float3(0, 0, -1); // Facing camera
                float3 lightDir = normalize(float3(0.5, 0.5, -1.0)); // Fake light direction

                // --- Fresnel Effect ---
                float fresnel = pow(1.0 - abs(dot(normal, float3(p, 0.5))), _FresnelPower);
                float3 fresnelColor = fresnel * _FresnelColor.rgb;

                // --- Specular Highlight ---
                float3 viewDir = float3(0, 0, -1);
                float3 reflectDir = reflect(lightDir, normal);
                float spec = pow(max(dot(viewDir, reflectDir), 0.0), _Shininess);
                float3 specularColor = spec * _SpecularColor.rgb;

                // --- Final Color ---
                float3 finalColor = _Color.rgb + fresnelColor + specularColor;

                return float4(finalColor, alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}
