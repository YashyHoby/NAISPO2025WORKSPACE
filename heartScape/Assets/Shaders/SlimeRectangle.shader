Shader "Custom/SlimeRectangle"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // スライムの基本色
        _SlimeColor ("Slime Color", Color) = (0.2, 0.8, 0.3, 1.0)
        
        // 長方形のサイズと形状
        _RectWidth ("Rectangle Width", Range(0.1, 1.0)) = 0.6
        _RectHeight ("Rectangle Height", Range(0.1, 1.0)) = 0.4
        _CornerRadius ("Corner Radius", Range(0.0, 0.3)) = 0.1
        
        // 外郭設定
        _OuterThickness ("Outer Thickness", Range(0.0, 0.2)) = 0.05
        _OuterAlpha ("Outer Alpha", Range(0.0, 1.0)) = 0.3
        _OuterColor ("Outer Color", Color) = (0.1, 0.6, 0.2, 1.0)
        
        // 内郭設定
        _InnerThickness ("Inner Thickness", Range(0.0, 0.2)) = 0.03
        _InnerAlpha ("Inner Alpha", Range(0.0, 1.0)) = 0.8
        _InnerColor ("Inner Color", Color) = (0.3, 0.9, 0.4, 1.0)
        
        // 光沢と粘性
        _Glossiness ("Glossiness", Range(0.0, 2.0)) = 1.0
        _Viscosity ("Viscosity", Range(0.0, 1.0)) = 0.8
        _SpecularPower ("Specular Power", Range(1.0, 100.0)) = 20.0
        
        // アニメーション
        _AnimationSpeed ("Animation Speed", Range(0.0, 5.0)) = 1.0
        _PulseIntensity ("Pulse Intensity", Range(0.0, 0.3)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _SlimeColor;
            float _RectWidth;
            float _RectHeight;
            float _CornerRadius;
            float _OuterThickness;
            float _OuterAlpha;
            fixed4 _OuterColor;
            float _InnerThickness;
            float _InnerAlpha;
            fixed4 _InnerColor;
            float _Glossiness;
            float _Viscosity;
            float _SpecularPower;
            float _AnimationSpeed;
            float _PulseIntensity;

            // 角丸長方形のSDF
            float sdRoundedBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            // ノイズ関数（粘性効果用）
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(lerp(hash(i), hash(i + float2(1.0, 0.0)), u.x),
                           lerp(hash(i + float2(0.0, 1.0)), hash(i + float2(1.0, 1.0)), u.x), u.y);
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.color = IN.color * _Color;
                OUT.texcoord = IN.texcoord;
                OUT.worldPos = IN.texcoord - 0.5; // 中心を原点に
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float2 p = IN.worldPos;
                float time = _Time.y * _AnimationSpeed;
                
                // アニメーション効果
                float pulse = 1.0 + _PulseIntensity * sin(time * 3.0);
                
                // 長方形のサイズ
                float2 rectSize = float2(_RectWidth, _RectHeight) * 0.5 * pulse;
                float cornerRadius = _CornerRadius * pulse;
                
                // 基本形状のSDF
                float baseDist = sdRoundedBox(p, rectSize, cornerRadius);
                
                // 外郭の距離
                float outerDist = sdRoundedBox(p, rectSize + _OuterThickness, cornerRadius + _OuterThickness * 0.5);
                
                // 内郭の距離
                float innerDist = sdRoundedBox(p, rectSize - _InnerThickness, cornerRadius);
                
                // 粘性効果のノイズ
                float viscosityNoise = noise(uv * 10.0 + time) * _Viscosity * 0.02;
                baseDist += viscosityNoise;
                outerDist += viscosityNoise * 0.5;
                innerDist += viscosityNoise * 0.3;
                
                // マスクの計算
                float outerMask = 1.0 - smoothstep(-0.01, 0.01, outerDist);
                float baseMask = 1.0 - smoothstep(-0.01, 0.01, baseDist);
                float innerMask = 1.0 - smoothstep(-0.01, 0.01, innerDist);
                
                // 各層の分離
                float outerOnly = outerMask - baseMask;
                float baseOnly = baseMask - innerMask;
                float innerOnly = innerMask;
                
                // 基本色の計算
                float3 finalColor = float3(0, 0, 0);
                float finalAlpha = 0;
                
                // 外郭（半透明）
                if (outerOnly > 0)
                {
                    finalColor += _OuterColor.rgb * outerOnly;
                    finalAlpha += _OuterAlpha * outerOnly;
                }
                
                // ベース部分
                if (baseOnly > 0)
                {
                    finalColor += _SlimeColor.rgb * baseOnly;
                    finalAlpha += _SlimeColor.a * baseOnly;
                }
                
                // 内郭
                if (innerOnly > 0)
                {
                    finalColor += _InnerColor.rgb * innerOnly;
                    finalAlpha += _InnerAlpha * innerOnly;
                }
                
                // 粘性のある液体風の光沢効果
                if (baseMask > 0)
                {
                    // 複数の光源による柔らかい光沢
                    float2 lightDir1 = normalize(float2(0.4, 0.8));
                    float2 lightDir2 = normalize(float2(-0.3, 0.6));
                    
                    // 表面の法線をノイズで微細に変化させる
                    float2 noiseOffset = float2(
                        noise(p * 20.0 + time * 2.0) - 0.5,
                        noise(p * 20.0 + time * 2.0 + 100.0) - 0.5
                    ) * 0.3;
                    float2 normal = normalize(p + noiseOffset);
                    
                    // 柔らかいスペキュラー反射
                    float specular1 = pow(max(0, dot(reflect(-lightDir1, normal), normalize(float2(0, 1)))), _SpecularPower * 0.5);
                    float specular2 = pow(max(0, dot(reflect(-lightDir2, normal), normalize(float2(-0.2, 1)))), _SpecularPower * 0.3);
                    
                    // 液体らしい柔らかい光沢色
                    float3 liquidSpecular = (specular1 + specular2 * 0.6) * _Glossiness;
                    liquidSpecular *= lerp(float3(1, 1, 1), _SlimeColor.rgb, 0.3); // 少し色を混ぜる
                    finalColor += liquidSpecular * baseMask;
                }
                
                // 液体の厚みによるフレネル効果
                if (baseMask > 0)
                {
                    float2 normal = normalize(p);
                    float fresnel = 1.0 - abs(dot(normal, float2(0, 1)));
                    fresnel = pow(fresnel, 1.5); // より柔らかいフレネル
                    
                    // 液体の内部散乱を模擬
                    float3 subsurfaceColor = _SlimeColor.rgb * 0.8;
                    float subsurface = pow(fresnel, 0.8) * _Viscosity;
                    
                    finalColor += subsurface * subsurfaceColor * 0.4 * baseMask;
                }
                
                // 液体表面の微細な反射
                if (baseMask > 0)
                {
                    float microReflection = noise(p * 50.0 + time * 3.0) * 0.1;
                    float3 microSpecular = microReflection * _Glossiness * 0.2;
                    finalColor += microSpecular * baseMask;
                }
                
                return fixed4(finalColor, finalAlpha);
            }
            ENDCG
        }
    }
}
