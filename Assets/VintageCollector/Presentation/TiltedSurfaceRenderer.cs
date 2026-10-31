using System;
using UnityEngine;
namespace VintageCollector.Presentation {
    public enum SurfaceFinish { Matte, Holo, Metallic }
    public sealed class TiltedSurfaceRenderer : IDisposable {
        Material material;
        bool disposed,attempted;
        static bool Finite(float v) {return !float.IsNaN(v)&&!float.IsInfinity(v);}
        static bool Valid(Rect r) {return Finite(r.x)&&Finite(r.y)&&Finite(r.width)&&Finite(r.height)&&r.width>0&&r.height>0&&Mathf.Abs(r.x)<10000000&&Mathf.Abs(r.y)<10000000&&r.width<10000000&&r.height<10000000;}
        static float Angle(float v) {return Finite(v)?Mathf.Clamp(v,-12f,12f):0f;}
        public bool Draw(Rect fittedRect,Texture2D face,Texture2D mask,SurfaceFinish finish,float pitchDegrees,float yawDegrees,float presentationClock) {
            if(disposed||Event.current==null||Event.current.type!=EventType.Repaint||face==null||!Valid(fittedRect))return false;
            if(!attempted) {
                attempted=true;var shader=Resources.Load<Shader>("Presentation/TiltedSurface");
                if(shader!=null&&shader.isSupported)material=new Material(shader) {hideFlags=HideFlags.HideAndDontSave};
            }
            if(material==null||!material.shader.isSupported)return false;
            Rect draw=ConservativeDrawRect(fittedRect);float size=Mathf.Max(fittedRect.width,fittedRect.height);
            bool aligned=mask!=null&&mask.width==face.width&&mask.height==face.height;
            material.SetTexture("_MainTex",face);material.SetTexture("_MaskTex",aligned?mask:Texture2D.blackTexture);
            material.SetVector("_SurfaceSize",new Vector4(fittedRect.width/size,fittedRect.height/size,draw.width/size,draw.height/size));
            material.SetVector("_RasterOffset",new Vector4((draw.center.x-fittedRect.center.x)/size,(fittedRect.center.y-draw.center.y)/size,0,0));
            material.SetVector("_Angles",new Vector4(Angle(pitchDegrees)*Mathf.Deg2Rad,Angle(yawDegrees)*Mathf.Deg2Rad,0,0));
            material.SetFloat("_Finish",aligned&&(finish==SurfaceFinish.Holo||finish==SurfaceFinish.Metallic)?(float)finish:0f);
            material.SetFloat("_PresentationClock",Finite(presentationClock)?presentationClock%4096f:0f);
            Graphics.DrawTexture(draw,face,material);return true;
        }
        public static Rect ConservativeDrawRect(Rect fittedRect) {
            if(!Valid(fittedRect))return new Rect();
            float pad=Mathf.Max(fittedRect.width,fittedRect.height)*.16f+2f;
            // Graphics.DrawTexture rasterizes integer edges. Explicit outward rounding and a
            // shader center offset avoid implicit truncation misaligning source UV at thin edges.
            float x=Mathf.Floor(fittedRect.x-pad),y=Mathf.Floor(fittedRect.y-pad);
            return Rect.MinMaxRect(x,y,Mathf.Ceil(fittedRect.xMax+pad),Mathf.Ceil(fittedRect.yMax+pad));
        }
        public void Dispose() {
            if(disposed)return;disposed=true;
            if(material!=null) {if(Application.isPlaying)UnityEngine.Object.Destroy(material);else UnityEngine.Object.DestroyImmediate(material);material=null;}
        }
    }
}
