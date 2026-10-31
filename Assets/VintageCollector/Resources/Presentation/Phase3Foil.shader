Shader "VintageCollector/Presentation/MaskedFoil"
{
    Properties {
        _MainTex ("Source", 2D) = "white" {}
        _MaskTex ("Explicit aligned red-channel times alpha mask", 2D) = "black" {}
        _PresentationTime ("Paused presentation clock", Float) = 0
    }
    SubShader {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MaskTex;
            float _PresentationTime;
            fixed4 frag(v2f_img i) : SV_Target {
                fixed4 mask = tex2D(_MaskTex,i.uv);
                float wave = 0.5 + 0.5*sin(i.uv.x*17 + i.uv.y*8 + _PresentationTime*1.25);
                fixed3 tint = lerp(fixed3(0.49,0.79,0.87),fixed3(0.95,0.81,0.44),wave);
                return fixed4(tint,mask.r*mask.a*(0.07+0.14*pow(wave,5)));
            }
            ENDCG
        }
    }
    Fallback Off
}
