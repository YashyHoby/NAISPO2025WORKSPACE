Shader "Custom/ParticleOutline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02
        _OutlineBrightness ("Outline Brightness", Range(0, 3)) = 1.5
        _Softness ("Softness", Range(0, 1)) = 0.5
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
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineBrightness;
            float _Softness;
            
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
                // メインテクスチャのサンプリング
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // アルファチャンネルで縁取りを検出
                float alpha = col.a;
                
                // 縁取りの幅を計算
                float outline = 0;
                float2 texelSize = 1.0 / 64.0; // テクスチャサイズに応じて調整
                
                // 8方向のサンプリングで縁取りを検出
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0) continue;
                        
                        float2 offset = float2(x, y) * _OutlineWidth * texelSize;
                        float sampleAlpha = tex2D(_MainTex, i.uv + offset).a;
                        
                        // 縁取り部分を検出（メイン部分は透明、周囲は不透明）
                        if (alpha < 0.5 && sampleAlpha > 0.5)
                        {
                            outline = 1.0;
                        }
                    }
                }
                
                // 縁取りの色を計算
                fixed4 outlineCol = _OutlineColor * _OutlineBrightness;
                outlineCol.a *= outline;
                
                // メイン部分の色
                fixed4 mainCol = col * _Color * i.color;
                
                // 縁取りとメイン部分を合成
                fixed4 finalCol = lerp(mainCol, outlineCol, outline);
                
                // ソフトネス効果を適用
                float softness = smoothstep(0, _Softness, alpha);
                finalCol.a *= softness;
                
                return finalCol;
            }
            ENDCG
        }
    }
}

