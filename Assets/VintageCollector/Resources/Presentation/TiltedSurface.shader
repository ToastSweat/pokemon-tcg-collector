Shader "VintageCollector/Presentation/TiltedSurface" {
    Properties {
        _MainTex ("Printed face, never modified",2D)="white" {}
        _MaskTex ("Aligned red times alpha finish mask",2D)="black" {}
        _Finish ("0 paper, 1 holo, 2 neutral foil",Float)=0
        _PresentationClock ("Caller-owned frozen clock",Float)=0
    }
    SubShader {
        Tags {"Queue"="Overlay" "RenderType"="Transparent"}
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex,_MaskTex;
            float4 _SurfaceSize,_Angles,_RasterOffset;
            float _Finish,_PresentationClock;
            float4 frag(v2f_img i):SV_Target {
                float sp,cp,sy,cy;sincos(_Angles.x,sp,cp);sincos(_Angles.y,sy,cy);
                float3 bx=float3(cy,0,-sy),by=float3(sy*sp,cp,cy*sp),n=float3(sy*cp,-sp,cy*cp);
                float3 camera=float3(0,0,2.4);
                float3 ray=float3((i.uv-.5)*_SurfaceSize.zw+_RasterOffset.xy,-2.4);
                float t=-dot(n,camera)/dot(n,ray);
                float3 p=camera+t*ray;
                float2 uv=float2(dot(p,bx),dot(p,by))/_SurfaceSize.xy+.5;
                clip(min(min(uv.x,1-uv.x),min(uv.y,1-uv.y)));
                float4 face=tex2D(_MainTex,uv);
                // A few percent of paper shading, not a full-face glare pass.
                float paper=1-.045*(1-n.z)-.012*abs(n.x)-.012*abs(n.y);
                float3 color=face.rgb*paper;
                if(_Finish>.5) {
                    // Same ray-plane UV for print and mask; missing/misaligned masks are rejected in C#.
                    float4 mask=tex2D(_MaskTex,uv);float coverage=saturate(mask.r*mask.a);
                    float3 view=normalize(camera-p);
                    float3 reflected=reflect(-view,n);
                    // Intersect reflected VIEW ray with a fixed virtual softbox plane z=2.
                    // Changing the normal moves this footprint even when the caller clock is frozen.
                    float2 light=p.xy+reflected.xy*((2-p.z)/max(reflected.z,.15));
                    float2 a=(light-float2(.35,.30))/float2(.75,.95);
                    float2 b=(light-float2(-1.25,-.6))/float2(.9,.65);
                    float softbox=exp(-dot(a*a,a*a))+.42*exp(-dot(b*b,b*b));
                    softbox=saturate(softbox);
                    if(_Finish<1.5) {
                        float phase=light.x*2.8+light.y*1.6+uv.x*3.5;
                        float3 spectrum=.58+.42*cos(phase+float3(0,2.094,4.189));
                        // Broad translucent iridescence retains source linework, not an opaque stripe.
                        color=color*(1-coverage*.10*softbox)+coverage*spectrum*(.018+.33*softbox);
                    }else {
                        // Achromatic added energy (including crimp masks), no rainbow on wrappers.
                        float headroom=max(0,.985-max(color.r,max(color.g,color.b)));
                        color+=coverage*min(headroom,.025+.43*softbox);
                    }
                }
                return float4(saturate(color),face.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
