Shader "Unlit/BubbleLiquid2D"
{
    Properties
    {
        _DyeTex("Dye (RGBA=4 channels)", 2D) = "black" {}
        _InnerAlpha("Inner Alpha", Range(0,1)) = 0.6
        _Softness("Edge Softness", Range(0,1)) = 0.2
        _FlowSpeed("Domain Warp Speed", Range(0,5)) = 0.7
        _FlowScale("Domain Warp Scale", Range(0.1,5)) = 1.6
        _Color0("Heart Color 0", Color) = (1,0.3,0.3,1)
        _Color1("Heart Color 1", Color) = (0.3,1,0.3,1)
        _Color2("Heart Color 2", Color) = (0.3,0.6,1,1)
        _Color3("Heart Color 3", Color) = (1,0.8,0.3,1)
    }
    SubShader
    {
        Tags{"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata{ float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f{ float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };

            TEXTURE2D(_DyeTex); SAMPLER(sampler_DyeTex);

            float _InnerAlpha, _Softness, _FlowSpeed, _FlowScale;
            float4 _Color0, _Color1, _Color2, _Color3;

            float noise(float2 p){
                float2 i=floor(p), f=frac(p);
                float a=frac(sin(dot(i,float2(127.1,311.7)))*43758.5453);
                float b=frac(sin(dot(i+float2(1,0),float2(127.1,311.7)))*43758.5453);
                float c=frac(sin(dot(i+float2(0,1),float2(127.1,311.7)))*43758.5453);
                float d=frac(sin(dot(i+float2(1,1),float2(127.1,311.7)))*43758.5453);
                float2 u=f*f*(3-2*f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            v2f vert(appdata v){ v2f o; o.pos=TransformObjectToHClip(v.vertex.xyz); o.uv=v.uv; o.color=v.color; return o; }

            float4 frag(v2f i):SV_Target
            {
                // バブル内部マスク（UV中心からの距離でフェード）
                float2 uvBall = i.uv*2-1;
                float r = length(uvBall);
                float mask = smoothstep(1.0, 1.0-_Softness, r); // 中央=1, 外側=0 になるよう反転
                mask = 1.0 - mask;

                // ドメインワープ（ゆらぎ座標で染料をサンプリング）
                float t = _Time.y * _FlowSpeed;
                float2 warp = float2(noise(i.uv*_FlowScale + t), noise(i.uv*_FlowScale*1.37 - t)) - 0.5;
                float2 suv = i.uv + warp*0.02;

                float4 w = SAMPLE_TEXTURE2D(_DyeTex, sampler_DyeTex, suv);
                // 勝者取り（argmax）
                float m = max(max(w.r, w.g), max(w.b, w.a));
                // ★染料が無いときは完全に透明（中身を描かない）
                if (m <= 1e-3) return float4(0,0,0,0);
                float3 col = (m==0)? float3(0,0,0) :
                            (m==w.r)? _Color0.rgb :
                            (m==w.g)? _Color1.rgb :
                            (m==w.b)? _Color2.rgb : _Color3.rgb;

                return float4(col, _InnerAlpha*mask);
            }
            ENDHLSL
        }
    }
}
