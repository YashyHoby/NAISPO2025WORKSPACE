Shader "Custom/ParticleTrail"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0, 3)) = 1.0
        _Saturation ("Saturation", Range(0, 2)) = 1.0
        _Alpha ("Alpha", Range(0, 1)) = 1.0
        _TrailWidth ("Trail Width", Range(0.01, 0.5)) = 0.05
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.0
        _FadeDistance ("Fade Distance", Range(0, 1)) = 0.5
        _EdgeSoftness ("Edge Softness", Range(0, 1)) = 0.8
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 screenPos : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Brightness;
            float _Saturation;
            float _Alpha;
            float _TrailWidth;
            float _GlowIntensity;
            float _FadeDistance;
            float _EdgeSoftness;
            
            // RGB to HSV conversion
            float3 rgb2hsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }
            
            // HSV to RGB conversion
            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.screenPos = ComputeScreenPos(o.vertex).xy;
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // テクスチャサンプリング
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                // ベースカラー
                fixed4 baseColor = texColor * i.color * _Color;
                
                // 明度調整
                baseColor.rgb *= _Brightness;
                
                // 彩度調整
                float3 hsv = rgb2hsv(baseColor.rgb);
                hsv.y *= _Saturation;
                hsv.y = saturate(hsv.y);
                baseColor.rgb = hsv2rgb(hsv);
                
                // アルファ調整
                baseColor.a *= _Alpha;
                
                // 軌跡の幅に基づくフェード効果
                float2 center = float2(0.5, 0.5);
                float distance = length(i.uv - center);
                float fade = 1.0 - smoothstep(_FadeDistance, 1.0, distance);
                
                // エッジソフトネス
                float edgeFade = smoothstep(0.0, _EdgeSoftness, fade);
                
                // グロー効果
                float glow = pow(fade, _GlowIntensity);
                baseColor.rgb += glow * baseColor.rgb * 0.5;
                
                // 最終アルファ
                baseColor.a *= edgeFade;
                
                return baseColor;
            }
            
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}