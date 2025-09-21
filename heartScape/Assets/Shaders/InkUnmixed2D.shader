Shader "Unlit/InkUnmixed2D"
{
    Properties
    {
        _MainTex ("Mask Sprite", 2D) = "white" {}
        _InkColor ("Ink Color", Color) = (1,0,0,1)
        _Alpha ("Max Alpha", Range(0,1)) = 0.9
        _NoiseScale ("Noise Scale", Range(0.5,10)) = 2
        _EdgeSoft ("Edge Softness", Range(0.001,0.2)) = 0.06
        _Threshold ("Fill Threshold", Range(0,1)) = 0.5
        _FlowDir ("Flow Dir XY", Vector) = (1,0,0,0)
        _Offset ("Flow Offset", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        // 既に描いた画素は占有済み：0の所だけ描く→描いたらインクリメント
        Stencil
        {
            Ref 0
            Comp Equal
            Pass IncrSat
            ReadMask 255
            WriteMask 255
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            float4 _InkColor;
            float _Alpha, _NoiseScale, _EdgeSoft, _Threshold;
            float4 _FlowDir, _Offset;

            float hash21(float2 p){ p=fract(p*float2(123.34,456.21)); p+=dot(p,p+45.32); return fract(p.x*p.y); }
            float noise(float2 p){ float2 i=floor(p), f=fract(p);
            float a=hash21(i), b=hash21(i+float2(1,0));
            float c=hash21(i+float2(0,1)), d=hash21(i+float2(1,1));
            float2 u=f*f*(3-2*f);
            return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y); }
            float fbm(float2 p){
            float s=0.0, a=0.5; for(int k=0;k<4;k++){ s+=a*noise(p); p*=2.02; a*=0.5; } return s;
            }

            v2f vert(appdata v){
                v2f o; o.pos=TransformObjectToHClip(v.vertex.xyz); o.uv=TRANSFORM_TEX(v.uv,_MainTex); o.color=v.color; return o;
            }

            half4 frag(v2f i):SV_Target
            {
                float4 mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                if (mask.a <= 0.001) discard;          // 丸の外は描かない

                float2 uv = i.uv * _NoiseScale + _Offset.xy + _FlowDir.xy * _Time.y;
                float n = fbm(uv);
                // しみ形状（しきい値で塗る／塗らないを決める）
                float m = smoothstep(_Threshold - _EdgeSoft, _Threshold + _EdgeSoft, n);
                float a = m * _Alpha;

                return float4(_InkColor.rgb, a);
            }
            ENDHLSL
        }
    }
}
