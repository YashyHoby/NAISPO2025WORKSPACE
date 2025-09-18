Shader "Unlit/AgentSDF"
{
    Properties
    {
        _Tint("Tint", Color) = (1,1,1,1)
        _Radius("Radius", Float) = 0.5
        _Pulse("Pulse", Float) = 0
        _ShapeType("ShapeType", Float) = 0 // 0:Circle 1:Tri 2:Box
        _Soft("Softness", Float) = 0.02
        _BoxSize("BoxSize", Vector) = (0.6,0.6,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            Blend One One
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _Tint;
            float _Radius;
            float _Pulse;
            float _ShapeType;
            float _Soft;
            float4 _BoxSize;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv * 2.0 - 1.0;
                return output;
            }

            float sdCircle(float2 p, float r)
            {
                return length(p) - r;
            }

            float sdBox(float2 p, float2 b)
            {
                float2 d = abs(p) - b;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
            }

            float sdEquiTri(float2 p, float r)
            {
                p.x = abs(p.x);
                return max((p.x * 0.5f + 0.288675f * p.y), -p.y) - r * 0.57735f;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * _Radius * (1.0f + _Pulse * 0.15f);
                float d;

                if (_ShapeType < 0.5f)
                {
                    d = sdCircle(p, _Radius);
                }
                else if (_ShapeType < 1.5f)
                {
                    d = sdEquiTri(p, _Radius);
                }
                else
                {
                    d = sdBox(p, _BoxSize.xy * _Radius);
                }

                float alpha = saturate(1.0f - smoothstep(0.0f, _Soft, d));
                return float4(_Tint.rgb * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
