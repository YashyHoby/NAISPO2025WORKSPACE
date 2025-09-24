Shader "Custom/SlimeBall"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (0.2, 0.8, 0.3, 1.0)
        _SlimeColor ("Slime Color", Color) = (0.1, 0.6, 0.2, 1.0)
        _HighlightColor ("Highlight Color", Color) = (0.4, 1.0, 0.5, 1.0)
        _NoiseScale ("Noise Scale", Range(0.5, 8.0)) = 2.0
        _NoiseIntensity ("Noise Intensity", Range(0.0, 1.0)) = 0.3
        _SlimeThickness ("Slime Thickness", Range(0.0, 0.5)) = 0.1
        _HighlightIntensity ("Highlight Intensity", Range(0.0, 2.0)) = 1.0
        _Wetness ("Wetness", Range(0.0, 1.0)) = 0.8
        _Bumpiness ("Bumpiness", Range(0.0, 1.0)) = 0.5
        _TimeScale ("Time Scale", Range(0.1, 3.0)) = 1.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
            };
            
            fixed4 _MainColor;
            fixed4 _SlimeColor;
            fixed4 _HighlightColor;
            float _NoiseScale;
            float _NoiseIntensity;
            float _SlimeThickness;
            float _HighlightIntensity;
            float _Wetness;
            float _Bumpiness;
            float _TimeScale;
            
            // ノイズ関数
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }
            
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(lerp(hash(i + float2(0.0, 0.0)), 
                                hash(i + float2(1.0, 0.0)), u.x),
                           lerp(hash(i + float2(0.0, 1.0)), 
                                hash(i + float2(1.0, 1.0)), u.x), u.y);
            }
            
            // フラクタルノイズ
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                return value;
            }
            
            // スライムの表面計算
            float slimeSurface(float2 uv, float time)
            {
                float2 noiseUV = uv * _NoiseScale + time * _TimeScale * 0.1;
                float surfaceNoise = fbm(noiseUV);
                
                // スライムの厚みを模擬
                float thickness = _SlimeThickness + surfaceNoise * _NoiseIntensity;
                
                return thickness;
            }
            
            // ハイライト計算
            float calculateHighlight(float3 viewDir, float3 normal, float2 uv, float time)
            {
                float fresnel = 1.0 - saturate(dot(viewDir, normal));
                float highlight = pow(fresnel, 2.0);
                
                // スライムの表面の凹凸によるハイライト
                float2 highlightUV = uv * _NoiseScale * 2.0 + time * _TimeScale * 0.2;
                float highlightNoise = fbm(highlightUV);
                highlight += highlightNoise * _Bumpiness;
                
                return saturate(highlight * _HighlightIntensity);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = normalize(mul(unity_ObjectToWorld, float4(v.normal, 0.0)).xyz);
                o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y * _TimeScale;
                
                // スライムの表面計算
                float slimeSurfaceValue = slimeSurface(uv, time);
                
                // ベースカラー
                float3 baseColor = lerp(_MainColor.rgb, _SlimeColor.rgb, slimeSurfaceValue);
                
                // ハイライト計算
                float highlight = calculateHighlight(i.viewDir, i.worldNormal, uv, time);
                float3 highlightColor = _HighlightColor.rgb * highlight;
                
                // ウェットネス効果
                float wetness = _Wetness * (1.0 + slimeSurfaceValue);
                baseColor = lerp(baseColor, baseColor * 1.2, wetness);
                
                // 最終色
                float3 finalColor = baseColor + highlightColor;
                
                // アルファ値（スライムの厚みに基づく）
                float alpha = lerp(_MainColor.a, _SlimeColor.a, slimeSurfaceValue);
                
                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
}
