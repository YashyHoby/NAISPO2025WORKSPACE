Shader "Unlit/BubbleIridescent2D"
{
    Properties
    {
        [PerRendererData]_MainTex ("Sprite", 2D) = "white" {}
        _BaseColor ("Base Tint", Color) = (1,1,1,1)
        _Alpha ("Alpha", Range(0,1)) = 0.2
        _RimPower ("Rim Power", Range(0.1,8)) = 2
        _IriIntensity ("Iridescence", Range(0,1)) = 0.85
        _Film ("Film Thickness", Range(0,2)) = 0.6
        _NoiseScale ("Distort Noise Scale", Range(0,10)) = 3
        _NoiseAmp ("Distort Noise Amp", Range(0,0.2)) = 0.04
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _BaseColor;
            float _Alpha, _RimPower, _IriIntensity, _Film, _NoiseScale, _NoiseAmp;

            // シンプルなノイズ
            float hash21(float2 p){ p=fract(p*float2(123.34,456.21)); p+=dot(p,p+45.32); return fract(p.x*p.y); }
            float noise(float2 p){ float2 i=floor(p), f=fract(p);
            float a=hash21(i), b=hash21(i+float2(1,0));
            float c=hash21(i+float2(0,1)), d=hash21(i+float2(1,1));
            float2 u=f*f*(3-2*f);
            return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y); }

            v2f vert (appdata v){
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv,_MainTex);
                o.color = v.color;
                return o;
            }

            float3 thinFilmRainbow(float rim, float film, float iri){
                // 擬似虹色（薄膜干渉っぽい色ずれ）
                float w = rim * (2.5 + film*3.0);
                float r = 0.5 + 0.5*sin(6.2831*(w+0.00));
                float g = 0.5 + 0.5*sin(6.2831*(w+0.33));
                float b = 0.5 + 0.5*sin(6.2831*(w+0.66));
                return lerp(float3(1,1,1), float3(r,g,b), iri);
            }

            half4 frag (v2f i) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                // 円スプライトのUV中心から半径
                float2 d = (i.uv - 0.5);
                float r = length(d)*2; // 約1で端
                // リム（縁）強調
                float rim = pow(saturate(1.0 - r), _RimPower);

                // 薄い歪み
                float n = noise(i.uv * _NoiseScale + _Time.y);
                float rimD = saturate(rim + (n-0.5)*_NoiseAmp);

                float3 iri = thinFilmRainbow(1.0 - rimD, _Film, _IriIntensity);
                float3 baseCol = _BaseColor.rgb * iri;

                // 中央を薄く、縁で少し強く
                float a = tex.a * ( _Alpha + (1.0 - saturate(r))*0.12 );
                return float4(baseCol, a);
            }
            ENDHLSL
        }
    }
}
