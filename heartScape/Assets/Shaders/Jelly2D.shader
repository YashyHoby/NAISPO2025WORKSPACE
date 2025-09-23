Shader "Unlit/Jelly2D"
{
    Properties
    {
        // Properties controlled by HeartVisual.cs
        _Tint ("Color", Color) = (1,1,1,1)
        _Smoothness("Smoothness", Range(0.0, 0.5)) = 0.1
        _OutlineWidth("Outline Width", Range(0.0, 0.1)) = 0.01
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _Refraction("Refraction", Range(0, 1)) = 0.1

        [Header(Jelly Feel)]
        _SpecularColor("Specular Color", Color) = (1,1,1,0.5)
        _Shininess("Shininess", Range(1, 100)) = 20
        _FresnelColor("Fresnel Color", Color) = (1,1,1,0.1)
        _FresnelPower("Fresnel Power", Range(0.1, 10.0)) = 2.0

        // Internal properties
        [HideInInspector] _VertexTex ("Vertex Texture", 2D) = "white" {}
        [HideInInspector] _IsCircle ("Is Circle", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define MAX_VERTICES 64

            struct appdata 
            { 
                float4 vertex : POSITION; 
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            // --- UNIFORMS ---
            float4 _Tint, _OutlineColor, _SpecularColor, _FresnelColor;
            float _Smoothness, _OutlineWidth, _Refraction, _Shininess, _FresnelPower, _IsCircle;

            sampler2D _GrabTexture; // From our custom GrabPassFeature
            sampler2D _VertexTex;
            int _VertexCount;

            // --- HELPER FUNCTIONS ---
            float2 getVertex(int i) { return tex2Dlod(_VertexTex, float4((i + 0.5) / MAX_VERTICES, 0.5, 0, 0)).rg; }
            float sdSegment(float2 p, float2 a, float2 b) { float2 pa = p-a, ba = b-a; float h = clamp(dot(pa,ba)/dot(ba,ba),0,1); return length(pa-ba*h); }
            float smin(float a, float b, float k) { float h=clamp(0.5+0.5*(b-a)/k,0,1); return lerp(b,a,h)-k*h*(1-h); }

            // --- SDF CALCULATION ---
            float sdPolygon(float2 p, int count)
            {
                float d = 1e10;
                int j = count - 1;
                for (int i = 0; i < count; i++) {
                    float segmentDist = sdSegment(p, getVertex(j), getVertex(i));
                    // If _IsCircle is 1, use hard min. Otherwise use smooth min.
                    d = lerp(smin(d, segmentDist, _Smoothness), min(d, segmentDist), _IsCircle);
                    j = i;
                }
                
                float wn = 0.0;
                for (int i = 0; i < count; ++i) {
                    float2 v1 = getVertex(i);
                    int next_i = i + 1;
                    if (next_i >= count) next_i = 0;
                    float2 v2 = getVertex(next_i);
                    if (v1.y <= p.y) {
                        if (v2.y > p.y && (v2.x - v1.x) * (p.y - v1.y) - (v2.y - v1.y) * (p.x - v1.x) > 0) wn += 1.0;
                    } else if (v2.y <= p.y && (v2.x - v1.x) * (p.y - v1.y) - (v2.y - v1.y) * (p.x - v1.x) < 0) {
                        wn -= 1.0;
                    }
                }
                return (wn == 0.0) ? d : -d;
            }

            // --- VERTEX SHADER ---
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeGrabScreenPos(o.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            // --- FRAGMENT SHADER ---
            float4 frag(v2f i) : SV_Target
            {
                float2 p = i.worldPos.xy;
                float d = sdPolygon(p, _VertexCount);
                float aa = fwidth(d) * 0.707;

                if (d > _OutlineWidth + aa * 2.0) discard;

                float2 epsilon = float2(0.001, 0);
                float2 g = normalize(float2(sdPolygon(p + epsilon.xy, _VertexCount) - d, sdPolygon(p + epsilon.yx, _VertexCount) - d));
                
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float2 refractedUV = screenUV + g * _Refraction * smoothstep(-0.1, 0.1, d);
                float4 bgColor = tex2D(_GrabTexture, refractedUV);
                
                float4 finalColor = bgColor;
                
                float outlineAlpha = smoothstep(aa, -aa, d - _OutlineWidth);
                finalColor = lerp(finalColor, _OutlineColor, outlineAlpha * _OutlineColor.a);
                
                float shapeAlpha = smoothstep(aa, -aa, d);
                finalColor = lerp(finalColor, _Tint, shapeAlpha * _Tint.a);

                // Add Jelly Feel: Specular & Fresnel
                float3 viewDir = normalize(float3(_WorldSpaceCameraPos.xy - p, _WorldSpaceCameraPos.z));
                float3 lightDir = normalize(float3(0.5, 0.5, 1.0)); // Fake light direction
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(saturate(dot(g, halfDir)), _Shininess);
                float fresnel = pow(1.0 - saturate(dot(float3(g, 0), viewDir)), _FresnelPower);

                finalColor.rgb += _SpecularColor.rgb * spec * _SpecularColor.a * shapeAlpha;
                finalColor.rgb += _FresnelColor.rgb * fresnel * _FresnelColor.a * shapeAlpha;

                finalColor.a = saturate(shapeAlpha * _Tint.a + outlineAlpha * _OutlineColor.a);
                
                if (finalColor.a < 0.01) discard;
                
                return finalColor;
            }
            ENDCG
        }
    }
}