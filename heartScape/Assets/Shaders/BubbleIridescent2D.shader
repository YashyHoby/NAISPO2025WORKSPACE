Shader "Unlit/BubbleShell2D"
{
    Properties
    {
        _ShellAlpha("Shell Alpha", Range(0,1)) = 0.35
        _RimPower("Rim Power", Range(0.5,8)) = 2.2
        _Iridescence("Iridescence Strength", Range(0,1)) = 0.35
        _Distort("Refraction Distortion", Range(0,2)) = 0.35
        _NoiseScale("Noise Scale", Range(0.1,10)) = 2.5
        _NoiseSpeed("Noise Speed", Range(0,5)) = 0.8
    }

    SubShader
    {
        Tags{"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float4 color : COLOR;
                float3 viewDirWS : TEXCOORD1;
                float3 worldPos  : TEXCOORD2;
            };

            float _ShellAlpha, _RimPower, _Iridescence, _Distort, _NoiseScale, _NoiseSpeed;

            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            float hash21(float2 p){ p = frac(p*float2(123.34,456.21)); p += dot(p,p+45.32); return frac(p.x*p.y); }
            float noise(float2 p){ // cheap value noise
                float2 i=floor(p), f=frac(p);
                float a=hash21(i);
                float b=hash21(i+float2(1,0));
                float c=hash21(i+float2(0,1));
                float d=hash21(i+float2(1,1));
                float2 u=f*f*(3-2*f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                o.color = v.color;
                float3 ws = TransformObjectToWorld(v.vertex.xyz);
                o.worldPos = ws;
                float3 camPos = GetCameraPositionWS();
                o.viewDirWS = normalize(camPos - ws);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // 円形スプライト前提：UV中心(0.5,0.5)からの半径でリムを作る
                float2 uv = i.uv * 2 - 1; // [-1,1]
                float r = saturate(length(uv));
                float rim = pow(saturate(1 - r), _RimPower);

                // のぞき込み屈折（簡易）: ノイズで背景サンプルUVをずらす
                float t = _Time.y * _NoiseSpeed;
                float2 dn = float2(noise(i.uv*_NoiseScale + t), noise(i.uv*_NoiseScale*1.231 - t));
                float2 offset = (dn - 0.5) * _Distort * 0.02; // 微小ずれ
                float2 screenUV = GetNormalizedScreenSpaceUV(i.pos);
                float4 bg = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + offset);

                // シャボン玉らしい虹色
                float3 irid = 0.5 + 0.5*cos(6.2831*(r + float3(0.0,0.33,0.66)));
                float3 shellCol = lerp(bg.rgb, bg.rgb + irid*_Iridescence, 0.6);

                float alpha = _ShellAlpha * (rim + 0.05); // 外周が少し濃い
                return float4(shellCol, alpha);
            }
            ENDHLSL
        }
    }
}
