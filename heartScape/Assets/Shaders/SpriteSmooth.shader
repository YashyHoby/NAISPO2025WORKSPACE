Shader "Sprites/Smooth"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.1
        _AntiAliasing ("Anti-Aliasing", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
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
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Smoothness;
            float _AntiAliasing;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.color = IN.color * _Color;
                OUT.texcoord = IN.texcoord;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // スムージング効果を適用
                float2 center = float2(0.5, 0.5);
                float2 dist = abs(IN.texcoord - center);
                float maxDist = max(dist.x, dist.y);
                
                // アンチエイリアシング
                float alpha = 1.0 - smoothstep(0.5 - _Smoothness, 0.5 + _Smoothness, maxDist);
                alpha = pow(alpha, _AntiAliasing);
                
                c.a *= alpha;
                c.rgb *= c.a;
                
                return c;
            }
            ENDCG
        }
    }
}
