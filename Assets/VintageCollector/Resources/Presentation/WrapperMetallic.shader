Shader "VintageCollector/Presentation/WrapperMetallic"
{
    Properties {
        _MainTex ("Source", 2D) = "white" {}
        _MaskTex ("Explicit aligned metallic red times alpha mask", 2D) = "black" {}
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
                fixed4 mask=tex2D(_MaskTex,i.uv);
                // Broad low-opacity silver response, not opaque/rainbow replacement of print.
                float glint=pow(0.5+0.5*sin(i.uv.x*5+i.uv.y*2-_PresentationTime*1.1),3);
                return fixed4(0.86,0.87,0.88,mask.r*mask.a*(0.025+0.115*glint));
            }
            ENDCG
        }
    }
    Fallback Off
}
