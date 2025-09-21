Shader "Unlit/BubbleLiquidVoronoi2D"
{
    Properties
    {
        _InnerAlpha("Inner Alpha", Range(0,1)) = 0.65
        _EdgeSoft ("Edge Softness", Range(0,0.5)) = 0.08
        _FlowSpeed("Domain Warp Speed", Range(0,5)) = 0.7
        _FlowScale("Domain Warp Scale", Range(0.1,5)) = 1.6
        _SiteCount("Site Count", Float) = 0
    }
    SubShader
    {
        Tags{"Queue"="Transparent" "RenderType"="Transparent" "UniversalMaterialType"="Unlit"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct app{ float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            #define MAX_SITES 16

            // ★ per-material はここだけ
            CBUFFER_START(UnityPerMaterial)
            float _InnerAlpha;
            float _EdgeSoft;
            float _FlowSpeed;
            float _FlowScale;
            float _SiteCount;
            float4 _Sites[MAX_SITES];    // xy=uv[0..1], z=丸み半径
            float4 _SiteCols[MAX_SITES]; // rgba=色
            CBUFFER_END

            float noise(float2 p){
                float2 i=floor(p), f=frac(p);
                float a=frac(sin(dot(i,float2(127.1,311.7)))*43758.5453);
                float b=frac(sin(dot(i+float2(1,0),float2(127.1,311.7)))*43758.5453);
                float c=frac(sin(dot(i+float2(0,1),float2(127.1,311.7)))*43758.5453);
                float d=frac(sin(dot(i+float2(1,1),float2(127.1,311.7)))*43758.5453);
                float2 u=f*f*(3-2*f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            v2f vert(app v){ v2f o; o.pos=TransformObjectToHClip(v.vertex.xyz); o.uv=v.uv; return o; }

            float4 frag(v2f i):SV_Target
            {
                // 円内マスク
                float2 uvBall = i.uv*2-1;
                float r = length(uvBall);
                float mask = 1.0 - smoothstep(1.0 - _EdgeSoft, 1.0, r);
                if (_SiteCount < 0.5 || mask <= 1e-3) return float4(0,0,0,0);

                // ドメインワープ
                float t = _Time.y * _FlowSpeed;
                float2 warp = float2(noise(i.uv*_FlowScale + t), noise(i.uv*_FlowScale*1.37 - t)) - 0.5;
                float2 p = i.uv + warp*0.02;

                // 丸みボロノイ
                float minVal = 1e9; int idx = 0;
                int sc = (int)_SiteCount;
                [unroll] for (int k=0; k<MAX_SITES; k++){
                    if (k >= sc) break;
                    float2 s = _Sites[k].xy; float rk = _Sites[k].z;
                    float val = length(p - s) - rk;
                    if (val < minVal){ minVal = val; idx = k; }
                }

                float a = saturate(1.0 - smoothstep(0.0, _EdgeSoft, minVal));
                float4 col = _SiteCols[idx];
                return float4(col.rgb, a * _InnerAlpha * mask);
            }
            ENDHLSL
        }
    }
}

