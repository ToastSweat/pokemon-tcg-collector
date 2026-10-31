using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using VintageCollector.Presentation;

public static class PresentationMotionTests
{
    private static readonly List<string> checks=new List<string>();
    private static void Check(bool ok,string name) { if(!ok) throw new Exception(name); checks.Add(name); }
    private static float Get(object m,string name) { return (float)m.GetType().GetProperty(name).GetValue(m,null); }
    private static void Call(object m,string name,params object[] args) { m.GetType().GetMethod(name).Invoke(m,args); }
    public static int Main()
    {
        string failure=null;
        try {
            var t=typeof(PresentationRules).Assembly.GetType("VintageCollector.Presentation.Phase3PresentationMotion");
            Check(t!=null,"Passive deterministic presentation motion exists");
            var m=Activator.CreateInstance(t);
            Check(Get(m,"Clock")==0 && Get(m,"RevealLift")==0,"No startup reveal motion");
            Call(m,"Advance",0.25f,true); Check(Get(m,"Clock")==0.25f,"Clock advances only by explicit frame delta");
            Call(m,"PulseReveal"); Check(Get(m,"RevealLift")==1,"Acknowledged reveal starts decoration immediately");
            Call(m,"Advance",10f,false); Check(Get(m,"Clock")==0.25f && Get(m,"RevealLift")==1,"Pause freezes clock and reveal at exact phase");
            Call(m,"Advance",0.14f,true); Check(Get(m,"RevealLift")>0 && Get(m,"RevealLift")<1,"Reveal decoration eases without changing a view");
            Call(m,"Inspect",2); Check(Get(m,"InspectionLift")==1,"Inspection transition starts restrained decoration");
            Call(m,"Advance",0.11f,true); float pulse=Get(m,"InspectionLift");
            Call(m,"Inspect",2); Check(Get(m,"InspectionLift")==pulse,"Unchanged view never restarts inspection animation");
            Call(m,"Advance",100f,false); Check(Get(m,"InspectionLift")==pulse,"Inspection freezes while paused");
            float before=Get(m,"Clock"); Call(m,"Advance",float.NaN,true); Call(m,"Advance",float.PositiveInfinity,true); Call(m,"Advance",-1f,true);
            Check(Get(m,"Clock")==before,"Invalid deltas cannot poison deterministic clock");
            Call(m,"Advance",1f,true); Check(Get(m,"RevealLift")==0 && Get(m,"InspectionLift")==0,"Decorations settle without pending action callbacks");
            var a=new OpeningView {stage="revealing",revealedCount=1,cards=new OpeningCardView[11]};
            Check(PresentationRules.CanEmit(a,"next") && PresentationRules.CanEmit(a,"skip"),"Reveal motion cannot gate next or skip");
            foreach(var size in new[]{new[]{1600,900},new[]{1280,360},new[]{2560,1080}}) {
                var l=PresentationRules.Layout(size[0],size[1]);
                foreach(var b in new[]{l.primary,l.secondary,l.pause,l.quit,l.resume,l.back,l.mute,l.quieter,l.louder})
                    Check(b.x>=0 && b.y>=0 && (b.x+b.width)*l.scale<=size[0]+0.01f && (b.y+b.height)*l.scale<=size[1]+0.01f,"Frozen control reachable "+size[0]+"x"+size[1]+" "+checks.Count);
            }
        } catch(Exception e) { failure=e.Message; }
        var names=new List<string>(); foreach(string n in checks) names.Add("\""+n+"\"");
        Console.WriteLine("{\"passed\":"+(failure==null?"true":"false")+",\"checkCount\":"+checks.Count+",\"checks\":["+string.Join(",",names.ToArray())+"],\"failure\":\""+(failure??"").Replace("\"","'")+"\"}");
        return failure==null?0:1;
    }
}
