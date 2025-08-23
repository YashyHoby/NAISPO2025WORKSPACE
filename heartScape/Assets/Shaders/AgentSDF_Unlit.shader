Shader "Unlit/AgentSDF"
{
  Properties{
    _Tint("Tint", Color) = (1,1,1,1)
    _Radius("Radius", Float) = 0.5
    _Pulse("Pulse", Float) = 0
    _ShapeType("ShapeType", Float) = 0 // 0:Circle 1:Tri 2:Box
    _Soft("Softness", Float) = 0.02
    _BoxSize("BoxSize", Vector) = (0.6,0.6,0,0)
  }
  SubShader{
    Tags{ "RenderType"="Transparent" "Queue"="Transparent" }
    Blend One One
    ZWrite Off
    Pass{
      HLSLPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"
      struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
      struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

      float4 _Tint; float _Radius; float _Pulse; float _ShapeType; float _Soft; float4 _BoxSize;

      v2f vert(appdata v){ v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv*2-1; return o; }

      float sdCircle(float2 p, float r){ return length(p)-r; }
      float sdBox(float2 p, float2 b){ float2 d=abs(p)-b; return length(max(d,0.0))+min(max(d.x,d.y),0.0); }
      float sdEquiTri(float2 p, float r){ // ê≥éOäpÇÃãﬂéó
        const float k = 1.7320508; // sqrt(3)
        p.x = abs(p.x);
        return max((p.x*0.5 + 0.288675*p.y), -p.y) - r*0.57735;
      }

      float4 frag(v2f i):SV_Target{
        float2 p = i.uv * _Radius * (1.0 + _Pulse*0.15); // îèìÆÇ≈î˜ägëÂ
        float d;
        if (_ShapeType < 0.5)       d = sdCircle(p, _Radius);
        else if (_ShapeType < 1.5)  d = sdEquiTri(p, _Radius);
        else                        d = sdBox(p, _BoxSize.xy*_Radius);

        float alpha = saturate(1.0 - smoothstep(0.0, _Soft, d));
        return float4(_Tint.rgb * alpha, alpha);
      }
      ENDHLSL
    }
  }
}
