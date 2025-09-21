Shader "Unlit/BubbleShellDeform2D"
{
    Properties
    {
        _ShellAlpha ("Shell Alpha", Range(0,1)) = 0.10
        _RimWidth   ("Rim Width",  Range(0.005,0.5)) = 0.12
        _RimPower   ("Rim Power",  Range(0.5,8)) = 2.2
        _RimTint    ("Rim Tint", Color) = (1,1,1,1)
        _Iridescence("Iridescence", Range(0,1)) = 0
        _NoiseScale ("Noise Scale", Range(0.1,10)) = 2.5
        _NoiseSpeed ("Noise Speed", Range(0,5)) = 0.6
        _ImpCount   ("Impulse Count", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "UniversalMaterialType"="Unlit" }
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

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };

            // ★ define は CBUFFER の外
            #define MAX_IMP 8

            // ★ ここにだけ per-material 変数を宣言（他の場所では宣言しないこと！）
            CBUFFER_START(UnityPerMaterial)
            float _ShellAlpha;
            float _RimWidth;
            float _RimPower;
            float _Iridescence;
            float _NoiseScale;
            float _NoiseSpeed;
            float _ImpCount;
            float4 _RimTint;
            float4 _Impulses[MAX_IMP]; // (xy=uv[0..1], z=amp(負=内側), w=radius)
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

            v2f vert(appdata v){
                v2f o; o.pos=TransformObjectToHClip(v.vertex.xyz); o.uv=v.uv; o.color=v.color; return o;
            }

            float4 frag(v2f i):SV_Target
            {
                float2 uv = i.uv*2-1;
                float r = length(uv);

                // 触点“へこみ”合成
                float deform = 0.0;
                int impCount = (int)_ImpCount;
                [unroll] for (int k=0; k<MAX_IMP; k++){
                    if (k >= impCount) break;
                    float2 p   = _Impulses[k].xy*2-1;
                    float amp  = _Impulses[k].z;
                    float rad  = max(1e-3, _Impulses[k].w);
                    float d    = length(uv - p) / rad;
                    float ga   = exp(-d*d*3.0);
                    deform += amp * ga;
                }

                // 外側はクリップされるよう saturate は使わない
                float rDef = r - deform;

                // 外側では必ず0になるゲート付きのリム
                float ringIn  = smoothstep(1.0 - _RimWidth, 1.0, rDef);
                float ringOut = 1.0 - step(1.0, rDef); // rDef>=1 → 0
                float ring    = pow(ringIn * ringOut, _RimPower);

                // 色（既定で虹はオフ）
                float t = _Time.y * _NoiseSpeed;
                float n = noise(i.uv*_NoiseScale + t);
                float3 irid = 0.5 + 0.5*cos(6.2831*(r + n + float3(0.0,0.33,0.66)));
                float3 col = lerp(_RimTint.rgb, _RimTint.rgb + irid*_Iridescence, _Iridescence);

                float a = _ShellAlpha * ring;
                if (a <= 1e-4) return float4(0,0,0,0);
                return float4(col, a) * i.color;
            }
            ENDHLSL
        }
    }
}
