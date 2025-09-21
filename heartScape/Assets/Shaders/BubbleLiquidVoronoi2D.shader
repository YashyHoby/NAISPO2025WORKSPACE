Shader "Unlit/BubbleLiquidVoronoi2D"
{
    Properties
    {
        _InnerAlpha ("Inner Alpha", Range(0,1)) = 0.65
        _EdgeSoft  ("Edge Softness", Range(0,0.5)) = 0.08
        _FlowSpeed ("Domain Warp Speed", Range(0,5)) = 0.7
        _FlowScale ("Domain Warp Scale", Range(0.1,5)) = 1.6
        _SiteCount ("Site Count", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            float _InnerAlpha;
            float _EdgeSoft;
            float _FlowSpeed;
            float _FlowScale;
            float _SiteCount;

            #define MAX_SITES 16
            float4 _Sites[MAX_SITES];    // xy = uv, z = radius, w unused
            float4 _SiteCols[MAX_SITES]; // rgba = colour

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = frac(sin(dot(i, float2(127.1, 311.7))) * 43758.5453);
                float b = frac(sin(dot(i + float2(1, 0), float2(127.1, 311.7))) * 43758.5453);
                float c = frac(sin(dot(i + float2(0, 1), float2(127.1, 311.7))) * 43758.5453);
                float d = frac(sin(dot(i + float2(1, 1), float2(127.1, 311.7))) * 43758.5453);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uvBall = input.uv * 2.0 - 1.0;
                float r = length(uvBall);
                float mask = 1.0 - smoothstep(1.0, 1.0 - _EdgeSoft, r);
                if (_SiteCount < 0.5 || mask <= 1e-4)
                    return float4(0, 0, 0, 0);

                float t = _Time.y * _FlowSpeed;
                float2 warp = float2(noise(input.uv * _FlowScale + t), noise(input.uv * _FlowScale * 1.37 - t)) - 0.5;
                float2 p = input.uv + warp * 0.02;

                float minVal = 1e9; int idx = 0;
                [unroll]
                for (int k = 0; k < MAX_SITES; ++k)
                {
                    if (k >= _SiteCount)
                        break;
                    float2 s = _Sites[k].xy;
                    float rad = max(1e-3, _Sites[k].z);
                    float val = length(p - s) - rad;
                    if (val < minVal)
                    {
                        minVal = val;
                        idx = k;
                    }
                }

                float alpha = saturate(1.0 - smoothstep(0.0, _EdgeSoft, minVal));
                float4 col = _SiteCols[idx];
                return float4(col.rgb, alpha * _InnerAlpha * mask) * input.color;
            }
            ENDHLSL
        }
    }
}
