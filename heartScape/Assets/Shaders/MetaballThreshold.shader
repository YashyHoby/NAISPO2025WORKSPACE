Shader "Hidden/MetaballThreshold"
{
  Properties{
    _MainTex ("Texture", 2D) = "white" {}
    _Thresh ("Threshold", Range(0,2)) = 0.6
    _Edge   ("EdgeWidth", Range(0,1)) = 0.15
  }
  SubShader{
    Tags{ "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
    Pass{
      Name "Metaball"
      ZTest Always ZWrite Off Cull Off
      HLSLPROGRAM
      #pragma vertex Vert
      #pragma fragment Frag
      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

      TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
      float4 _MainTex_TexelSize;
      float _Thresh, _Edge;

      struct VIn { float4 pos:POSITION; float2 uv:TEXCOORD0; };
      struct VOut{ float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

      VOut Vert(VIn v){ VOut o; o.pos = TransformObjectToHClip(v.pos.xyz); o.uv = v.uv; return o; }

      float Luma(float3 c){ return dot(c, float3(0.2126,0.7152,0.0722)); }

      float4 Frag(VOut i):SV_Target{
        float3 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
        float v = Luma(col); // ���Z���ʂ̖��邳���T�C�������
        float body = smoothstep(_Thresh - 0.05, _Thresh + 0.05, v);
        // �ߖT�T���v���ŊȈՃG�b�W
        float3 dx = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv + float2(_MainTex_TexelSize.x,0)).rgb;
        float3 dy = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv + float2(0,_MainTex_TexelSize.y)).rgb;
        float g = abs(Luma(dx)-Luma(col)) + abs(Luma(dy)-Luma(col));
        float edge = smoothstep(_Edge, 0.0, g);
        float3 outCol = col * body + edge.xxx;
        return float4(outCol, 1);
      }
      ENDHLSL
    }
  }
}
　