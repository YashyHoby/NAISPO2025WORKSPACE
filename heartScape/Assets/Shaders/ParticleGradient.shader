Shader "Custom/ParticleGradient"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _GradientPower ("Gradient Power", Range(0.1, 5.0)) = 2.0
        _CenterBrightness ("Center Brightness", Range(0, 3)) = 1.5
        _EdgeSoftness ("Edge Softness", Range(0, 1)) = 0.8
        _FadeDistance ("Fade Distance", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
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
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _GradientPower;
            float _CenterBrightness;
            float _EdgeSoftness;
            float _FadeDistance;
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 中心からの距離を計算（UV座標の中心は0.5, 0.5）
                float2 center = float2(0.5, 0.5);
                float2 centerToUV = i.uv - center;
                float distance = length(centerToUV);
                
                // 円形のグラデーション効果を計算
                float gradient = 1.0 - pow(saturate(distance * 2.0), _GradientPower);
                
                // 中心の明るさを適用
                gradient *= _CenterBrightness;
                
                // エッジのソフトネスを適用
                float edgeFade = smoothstep(_FadeDistance, 1.0, distance);
                gradient *= (1.0 - edgeFade * _EdgeSoftness);
                
                // 円形のマスクを適用
                float circleMask = 1.0 - smoothstep(0.4, 0.5, distance);
                gradient *= circleMask;
                
                // 色を適用
                fixed4 col = _Color * i.color;
                col.rgb *= gradient;
                col.a *= gradient;
                
                return col;
            }
            ENDCG
        }
    }
}
