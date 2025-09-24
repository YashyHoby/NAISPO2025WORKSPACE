Shader "Custom/BackgroundDepth"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (0.05, 0.4, 0.7, 1.0)
        _SecondColor ("Second Color", Color) = (0.02, 0.15, 0.3, 1.0)
        _AccentColor ("Accent Color", Color) = (0.3, 0.9, 1.0, 1.0)
        _DepthFalloff ("Depth Falloff", Range(0.5, 8.0)) = 3.0
        _WaveSpeed ("Wave Speed", Range(0.2, 2.0)) = 0.8
        _WaveScale ("Wave Scale", Range(1.0, 15.0)) = 5.0
        _NoiseScale ("Noise Scale", Range(0.5, 8.0)) = 2.5
        _CausticsIntensity ("Caustics Intensity", Range(0.0, 2.0)) = 1.0
        _LightIntensity ("Light Intensity", Range(0.0, 3.0)) = 1.5
        _DistortionStrength ("Distortion Strength", Range(0.0, 0.5)) = 0.1
        _FresnelPower ("Fresnel Power", Range(0.5, 5.0)) = 2.0
        
        _LiquidIntensity ("Liquid Intensity", Range(0.0, 2.0)) = 1.0
        _LiquidSpeed ("Liquid Speed", Range(0.1, 3.0)) = 1.2
        _LiquidComplexity ("Liquid Complexity", Range(1.0, 8.0)) = 4.0
        _LiquidViscosity ("Liquid Viscosity", Range(0.1, 2.0)) = 0.8
        
        _DepthLayers ("Depth Layers", Range(2, 8)) = 4
        _DepthIntensity ("Depth Intensity", Range(0.5, 3.0)) = 1.5
        _ParallaxStrength ("Parallax Strength", Range(0.0, 1.0)) = 0.3
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
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
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };
            
            fixed4 _MainColor;
            fixed4 _SecondColor;
            fixed4 _AccentColor;
            float _DepthFalloff;
            float _WaveSpeed;
            float _WaveScale;
            float _NoiseScale;
            float _CausticsIntensity;
            float _LightIntensity;
            float _DistortionStrength;
            float _FresnelPower;
            
            // Liquid Motion
            float _LiquidIntensity;
            float _LiquidSpeed;
            float _LiquidComplexity;
            float _LiquidViscosity;
            
            // 3D Depth
            float _DepthLayers;
            float _DepthIntensity;
            float _ParallaxStrength;
            
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
                
                for(int i = 0; i < 6; i++)
                {
                    value += amplitude * noise(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                return value;
            }
            
            // 液体のような滑らかな揺れ計算
            float2 liquidDistortion(float2 uv, float time)
            {
                float2 distortion = float2(0, 0);
                
                // 複数レイヤーの液体揺れ
                for(int i = 0; i < 4; i++)
                {
                    float layer = float(i) + 1.0;
                    float layerTime = time * _LiquidSpeed * (0.5 + layer * 0.3);
                    float layerScale = _WaveScale * layer * 0.5;
                    
                    // 滑らかな液体揺れ（振幅を制限）
                    float2 layerDistortion = float2(
                        sin(uv.x * layerScale + layerTime) * (1.0 / layer) * 0.1,
                        cos(uv.y * layerScale * 0.8 + layerTime * 1.2) * (1.0 / layer) * 0.1
                    );
                    
                    // 粘性による減衰
                    layerDistortion *= exp(-layer * _LiquidViscosity * 0.3);
                    distortion += layerDistortion;
                }
                
                // 複雑な液体パターン（振幅を制限）
                float2 complexPattern = float2(
                    fbm(uv * _LiquidComplexity + time * _LiquidSpeed * 0.5),
                    fbm(uv * _LiquidComplexity * 1.3 + time * _LiquidSpeed * 0.7 + float2(100, 200))
                );
                
                distortion += complexPattern * 0.01; // 大幅に減衰
                
                // 歪みを画面内に制限
                distortion = clamp(distortion, -0.1, 0.1);
                
                return distortion * _LiquidIntensity * _DistortionStrength;
            }
            
            // 水中の歪み計算（従来版との互換性）
            float2 waterDistortion(float2 uv, float time)
            {
                return liquidDistortion(uv, time);
            }
            
            // 水中のコースティクス（光の模様）- より滑らか
            float caustics(float2 uv, float time)
            {
                float2 causticUV = uv * _NoiseScale + time * _WaveSpeed * 0.3;
                
                float caustic1 = fbm(causticUV);
                float caustic2 = fbm(causticUV * 1.5 + time * 0.2);
                float caustic3 = fbm(causticUV * 2.5 + time * 0.4);
                
                float causticPattern = (caustic1 + caustic2 * 0.6 + caustic3 * 0.3) / 1.9;
                return smoothstep(0.3, 0.8, causticPattern) * _CausticsIntensity;
            }
            
            // 立体感を強化する深度計算
            float calculateDepth(float2 uv, float time)
            {
                float depth = 0.0;
                
                // 複数レイヤーの深度計算
                for(int i = 0; i < int(_DepthLayers); i++)
                {
                    float layer = float(i) / (_DepthLayers - 1.0);
                    float layerTime = time * _WaveSpeed * (0.3 + layer * 0.7);
                    float layerScale = _WaveScale * (0.5 + layer * 1.5);
                    
                    // 各レイヤーの深度
                    float layerDepth = sin(uv.x * layerScale + layerTime) * 0.5 + 0.5;
                    layerDepth *= cos(uv.y * layerScale * 0.7 + layerTime * 1.3) * 0.5 + 0.5;
                    
                    // レイヤー重み
                    float layerWeight = 1.0 - layer;
                    depth += layerDepth * layerWeight;
                }
                
                // パララックス効果
                float2 parallaxOffset = liquidDistortion(uv, time) * _ParallaxStrength;
                float parallaxDepth = fbm(uv + parallaxOffset * 0.5) * 0.3;
                depth += parallaxDepth;
                
                return saturate(depth / _DepthLayers * _DepthIntensity);
            }
            
            // フレネル効果
            float fresnel(float3 viewDir, float3 normal)
            {
                float fresnel = 1.0 - saturate(dot(viewDir, normal));
                return pow(fresnel, _FresnelPower);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y * _WaveSpeed;
                
                // 水中の歪みを適用
                float2 distortion = waterDistortion(uv, time);
                float2 distortedUV = uv + distortion;
                
                // 画面境界を超えないように制限
                distortedUV = clamp(distortedUV, 0.0, 1.0);
                
                // 立体感を強化した深度計算
                float depth = calculateDepth(distortedUV, time);
                float falloffDepth = pow(1.0 - distortedUV.y, _DepthFalloff);
                float combinedDepth = saturate(depth + falloffDepth * 0.5);
                float3 baseColor = lerp(_SecondColor.rgb, _MainColor.rgb, combinedDepth);
                
                // 水中の波の動き（より複雑で滑らか）
                float wave1 = sin(distortedUV.x * _WaveScale + time) * 0.5 + 0.5;
                float wave2 = cos(distortedUV.y * _WaveScale * 0.7 + time * 1.3) * 0.5 + 0.5;
                float wave3 = sin(distortedUV.x * _WaveScale * 1.3 + distortedUV.y * _WaveScale * 0.9 + time * 0.8) * 0.3 + 0.7;
                float wavePattern = (wave1 + wave2 + wave3) / 3.0;
                
                // 水中のコースティクス（光の模様）
                float causticPattern = caustics(distortedUV, time);
                
                // ノイズによる複雑な模様（より滑らか）
                float2 noiseUV = distortedUV * _NoiseScale + time * 0.1;
                float noiseValue = fbm(noiseUV);
                
                // フレネル効果の計算
                float3 viewDir = normalize(float3(0, 0, 1));
                float3 normal = normalize(float3(distortion.x, distortion.y, 1.0));
                float fresnelEffect = fresnel(viewDir, normal);
                
                // 最終的な背景色
                float3 finalColor = baseColor;
                
                // 波のパターンを適用
                finalColor = lerp(finalColor, _AccentColor.rgb, wavePattern * 0.3);
                
                // コースティクスを適用
                finalColor += _AccentColor.rgb * causticPattern * 0.4;
                
                // ノイズによる微細な変化
                finalColor = lerp(finalColor, _MainColor.rgb, noiseValue * 0.2);
                
                // 光の揺らぎ（より滑らかでちらつきを抑制）
                float2 lightPos1 = float2(0.3, 0.7) + 0.1 * sin(time * 0.8 + float2(1.0, 2.0));
                float2 lightPos2 = float2(0.7, 0.3) + 0.1 * cos(time * 0.6 + float2(3.0, 1.5));
                
                float dist1 = length(distortedUV - lightPos1);
                float dist2 = length(distortedUV - lightPos2);
                
                float flicker1 = (1.0 + 0.2 * sin(time * 2.0 + dist1 * 3.0)) / (1.0 + dist1 * 1.5);
                float flicker2 = (1.0 + 0.15 * cos(time * 1.5 + dist2 * 2.5)) / (1.0 + dist2 * 2.0);
                
                float3 lightFlicker = _AccentColor.rgb * (flicker1 + flicker2) * _LightIntensity * 0.3;
                finalColor += lightFlicker;
                
                // フレネル効果を適用
                finalColor = lerp(finalColor, _AccentColor.rgb, fresnelEffect * 0.3);
                
                // 浮遊パーティクルを削除（ちらつきの原因）
                
                // 水中の透明度効果（より滑らか）
                float transparency = 0.9 + 0.1 * sin(time * 0.2 + noiseValue * 0.5);
                finalColor *= transparency;
                
                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}